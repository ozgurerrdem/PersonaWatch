using System.Diagnostics;
using System.Net.Http.Headers;
using PersonaWatch.Application.Abstraction;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace PersonaWatch.Infrastructure.Providers.Media;

public sealed class ClipService : IClipService
{
    public async Task<ClipResult> ClipAsync(
    string videoId, int start, int end, CancellationToken ct = default)
    {
        var ffmpegPath = FindTool("FFMPEG_PATH", "ffmpeg");
        var ytdlpPath = FindTool("YTDLP_PATH", "yt-dlp");

        if (string.IsNullOrWhiteSpace(ffmpegPath))
            throw new InvalidOperationException("ffmpeg bulunamadı.");

        var tmpFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mp4");
        var safeName = MakeSafeFileName($"{DateTime.Now.Ticks}_{start}-{end}.mp4");

        try
        {
            // 1) Önce yt-dlp ile deneyelim
            if (!string.IsNullOrWhiteSpace(ytdlpPath))
            {
                try
                {
                    var ytdlpArgs =
                        $"-f \"bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]\" " +
                        $"--download-sections \"*{start}-{end}\" " +
                        $"--force-keyframes-at-cuts " +
                        $"--retries 3 --fragment-retries 3 " +
                        $"--no-part --no-continue -o \"{tmpFile}\" " +
                        $"\"https://www.youtube.com/watch?v={videoId}\"";

                    await RunProcessAsync(ytdlpPath!, ytdlpArgs, ct);

                    if (File.Exists(tmpFile) && new FileInfo(tmpFile).Length > 0)
                    {
                        var ytDlpStream = new FileStream(tmpFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16,
                            FileOptions.DeleteOnClose);
                        return new ClipResult(ytDlpStream, safeName, "video/mp4");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"yt-dlp failed, falling back to FFmpeg: {ex.Message}");
                    // Devam et, FFmpeg'i dene
                }
            }

            // 2) FFmpeg fallback
            Console.WriteLine("Using FFmpeg fallback...");
            var (videoUrl, audioUrl) = await TryResolveStreamsAsync(videoId, ytdlpPath, ct);
            if (videoUrl is null)
                throw new InvalidOperationException("Video akış URL'si alınamadı.");

            var isHlsOrDash = videoUrl.Contains("m3u8", StringComparison.OrdinalIgnoreCase) ||
                              videoUrl.Contains("dash", StringComparison.OrdinalIgnoreCase);

            // Build the base arguments with input options FIRST
            string inputOptions = (isHlsOrDash ? "-seek_timestamp 1 " : "") + $"-ss {start} -t {end - start}";

            string args;
            if (audioUrl == null)
            {
                // Muxed (tek URL)
                // Place input options BEFORE the -i input
                args = $"{inputOptions} -i \"{videoUrl}\" " +
                       $"-filter_complex \"[0:v]trim=start=0:end={end - start},setpts=PTS-STARTPTS[v];" +
                       $"[0:a]atrim=start=0:end={end - start},asetpts=PTS-STARTPTS[a]\" " +
                       "-map \"[v]\" -map \"[a]\" " +
                       "-c:v libx264 -c:a aac -preset veryfast -shortest " +
                       "-movflags +faststart " +
                       "-avoid_negative_ts make_zero -reset_timestamps 1 " +
                       $"-loglevel error -y \"{tmpFile}\"";
            }
            else
            {
                // Adaptif (ayrı video + audio)
                // Place input options BEFORE the first -i input
                args = $"{inputOptions} -i \"{videoUrl}\" -i \"{audioUrl}\" " +
                       $"-filter_complex \"[0:v]trim=start=0:end={end - start},setpts=PTS-STARTPTS[v];" +
                       $"[1:a]atrim=start=0:end={end - start},asetpts=PTS-STARTPTS[a]\" " +
                       "-map \"[v]\" -map \"[a]\" " +
                       "-c:v libx264 -c:a aac -preset veryfast -shortest " +
                       "-movflags +faststart " +
                       "-avoid_negative_ts make_zero -reset_timestamps 1 " +
                       $"-loglevel error -y \"{tmpFile}\"";
            }

            await RunProcessAsync(ffmpegPath!, args, ct);

            if (!File.Exists(tmpFile) || new FileInfo(tmpFile).Length == 0)
                throw new InvalidOperationException("Video klibi oluşturulamadı.");

            var ffmpegStream = new FileStream(tmpFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16,
                FileOptions.DeleteOnClose);
            return new ClipResult(ffmpegStream, safeName, "video/mp4");
        }
        catch
        {
            // Hata durumunda temp dosyayı temizle
            try { if (File.Exists(tmpFile)) File.Delete(tmpFile); } catch { }
            throw;
        }
    }

    // ---- Yardımcı Metotlar ----

    private static async Task RunProcessAsync(string file, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        process.Start();

        // Process başladıktan SONRA stream'lere eriş
        var errorTask = process.StandardError.ReadToEndAsync();
        var outputTask = process.StandardOutput.ReadToEndAsync();

        var error = await errorTask;
        var output = await outputTask;

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(file)} hata ile döndü (ExitCode: {process.ExitCode}).\n" +
                $"STDERR: {error}\nSTDOUT: {output}");
        }

        // Debug için
        if (!string.IsNullOrEmpty(error))
            Console.WriteLine($"{Path.GetFileName(file)} STDERR: {error}");
    }

    private async Task<(string? videoUrl, string? audioUrl)> TryResolveStreamsAsync(
        string videoId, string? ytdlpPath, CancellationToken ct)
    {
        // 1) yt-dlp ile çözümle
        if (!string.IsNullOrWhiteSpace(ytdlpPath))
        {
            try
            {
                var (v, a) = await ResolveWithYtDlpAsync(videoId, ytdlpPath, ct);
                if (v != null) return (v, a);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"yt-dlp resolve failed: {ex.Message}");
            }
        }

        // 2) YoutubeExplode fallback
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.Clear();
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "124.0.0.0"));
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));

            var yt = new YoutubeClient(http);
            var manifest = await yt.Videos.Streams.GetManifestAsync(new VideoId(videoId), ct);

            // MP4 muxed varsa onu seç
            var muxed = manifest.GetMuxedStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestVideoQuality();
            if (muxed != null) return (muxed.Url, null);

            // Adaptif fallback
            var bestVideo = manifest.GetVideoStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestVideoQuality();
            var bestAudio = manifest.GetAudioStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestBitrate();

            if (bestVideo != null && bestAudio != null) return (bestVideo.Url, bestAudio.Url);

            var anyVideo = manifest.GetVideoStreams().GetWithHighestVideoQuality();
            if (anyVideo != null) return (anyVideo.Url, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"YoutubeExplode failed: {ex.Message}");
        }

        return (null, null);
    }

    private async Task<(string? videoUrl, string? audioUrl)> ResolveWithYtDlpAsync(
        string videoId, string ytdlpPath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ytdlpPath,
            Arguments = $"-f \"best[height<=720]\" -g \"https://www.youtube.com/watch?v={videoId}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(ct);

        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
        {
            var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 1) return (lines[0], null);
            if (lines.Length >= 2) return (lines[0], lines[1]);
        }

        Console.WriteLine($"yt-dlp resolve failed: {stderr}");
        return (null, null);
    }

    private static string? FindTool(string envVarName, string defaultName)
    {
        var explicitPath = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            return explicitPath;

        try
        {
            var which = OperatingSystem.IsWindows() ? "where" : "which";
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = which,
                Arguments = defaultName,
                RedirectStandardOutput = true,
                UseShellExecute = false
            })!;
            var path = p.StandardOutput.ReadToEnd().Split('\n', '\r', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return path;
        }
        catch { }

        return defaultName;
    }

    private static string MakeSafeFileName(string name)
    {
        var invalids = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}