using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonaWatch.Application.Abstraction.Media;
using PersonaWatch.Application.Abstraction.Services;
using PersonaWatch.Application.Services;
using PersonaWatch.Infrastructure.Persistence;
using PersonaWatch.Infrastructure.Providers.Apify;
using PersonaWatch.Infrastructure.Providers.Media;
using PersonaWatch.Infrastructure.Providers.Reports;
using PersonaWatch.Infrastructure.Providers.Scanners;
using PersonaWatch.Infrastructure.Providers.Scanners.Apify;
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

        // === REPORTS (Yeni) ===
        services.AddScoped<ReportService>();
        services.AddScoped<IReports, InstagramReportService>();

        // === IClip implementasyonu ===
        services.AddScoped<IClipService, ClipService>();

        // === IScanner implementasyonları ===
        services.AddScoped<IScanner, SerpApiScannerService>();
        services.AddScoped<IScanner, YouTubeScannerService>();
        services.AddScoped<IScanner, FilmotScannerService>();
        services.AddScoped<IScanner, EksiScannerService>();
        services.AddScoped<IScanner, SikayetvarScannerService>();

        // === Apify tabanlı scanner’lar ===
        services.AddScoped<IScanner, XApifyScannerService>();
        services.AddScoped<IScanner, InstagramApifyScannerService>();
        services.AddScoped<IScanner, FacebookApifyScannerService>();
        services.AddScoped<IScanner, TiktokApifyScannerService>();

        // === Uygulama servisleri ===
        services.AddScoped<TokenService>();
        services.AddScoped<ScanService>(); 
        
        return services;
    }
}