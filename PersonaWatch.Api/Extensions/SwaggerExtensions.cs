namespace PersonaWatch.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        services.AddSwaggerGen();
        return services;
    }

    public static IApplicationBuilder UseOpenApiIfDev(this IApplicationBuilder app, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        return app;
    }
}
