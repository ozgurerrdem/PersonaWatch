using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Infrastructure.Persistence;
using PersonaWatch.Infrastructure.Providers.Apify;
using PersonaWatch.Infrastructure.Providers.Media;
using PersonaWatch.Infrastructure.Providers.NewsContent;
using PersonaWatch.Infrastructure.Providers.Report;
using PersonaWatch.Infrastructure.Providers.Reporter;
using PersonaWatch.Infrastructure.Providers.Scan;
using PersonaWatch.Infrastructure.Providers.Scanners;
using PersonaWatch.Infrastructure.Providers.Scanners.Apify;
using PersonaWatch.Infrastructure.Providers.Scanners.RapidApi;
using PersonaWatch.Infrastructure.Providers.User;
using PersonaWatch.Infrastructure.Providers.UserProfiles;
using PersonaWatch.Infrastructure.Security;

namespace PersonaWatch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // === DbContext ===
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        // === HttpClient’lar ===
        services.AddHttpClient();
        services.AddHttpClient<ApifyClient>();
        services.AddHttpClient<EksiScannerService>();

        // === Token ===
        services.AddScoped<IToken, TokenService>();

        // === User ===
        services.AddScoped<IUser, UserService>();

        // === NewsContent ===
        services.AddScoped<INewsContent, NewsContentService>();

        // === Scan ===
        services.AddScoped<IScan, ScanService>();

        // === Scanner ===
        services.AddScoped<IScanner, SerpApiScannerService>();
        services.AddScoped<IScanner, YouTubeScannerService>();
        services.AddScoped<IScanner, FilmotScannerService>();
        services.AddScoped<IScanner, FilmotRapidApiScannerService>();
        services.AddScoped<FilmotRapidApiScannerService>();
        services.AddScoped<IScanner, EksiScannerService>();
        services.AddScoped<IScanner, SikayetvarScannerService>();
        services.AddScoped<IScanner, XApifyScannerService>();
        services.AddScoped<IScanner, InstagramApifyScannerService>();
        services.AddScoped<IScanner, FacebookApifyScannerService>();
        services.AddScoped<IScanner, TiktokApifyScannerService>();

        // === Clip ===
        services.AddScoped<IClipService, ClipService>();

        // === UserProfiles ===
        services.AddScoped<IUserProfiles, UserProfilesService>();
        
        // === Report ===
        services.AddScoped<IReport, ReportService>();

        // === Reporter ===
        services.AddScoped<IReporter, InstagramReporterService>();
        services.AddScoped<IReporter, XReporterService>();

        return services;
    }
}