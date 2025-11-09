namespace PersonaWatch.Api.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration cfg, string policyName = "AllowFrontend")
    {
        var origin = cfg["Cors:FrontendOrigin"];
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, p =>
            {
                p.WithOrigins(origin!)
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
            });
        });
        return services;
    }
}
