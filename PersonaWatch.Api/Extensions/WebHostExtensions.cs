using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

namespace PersonaWatch.Api.Extensions;

public static class WebHostExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // === DbContext ===
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        // === HttpClient’lar ===
        services.AddHttpClient();                    // generic factory
        services.AddHttpClient<ApifyClient>();
        services.AddHttpClient<EksiScannerService>();

        // === Uygulama servisleri ===
        services.AddScoped<TokenService>();
        services.AddScoped<ScanService>();

        // === REPORTS (Yeni) ===
        services.AddScoped<ReportService>();                 // Orkestratör
        services.AddScoped<IReports, InstagramReportService>(); // Instagram implementasyonu
        // İleride eklenecek diğerleri:
        // services.AddScoped<IReports, XReportService>();
        // services.AddScoped<IReports, FacebookReportService>();
        // services.AddScoped<IReports, TikTokReportService>();

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

        // === Authentication / JWT ===
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = configuration["Jwt:Issuer"],
                    ValidAudience            = configuration["Jwt:Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)
                    )
                };
            });

        return services;
    }
}

