using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PersonaWatch.Api.Extensions;
using PersonaWatch.Api.Security;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Infrastructure;
using PersonaWatch.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// DI kümeleri
builder.Services.AddPresentation(addGlobalAuthFilter: false);
builder.Services.AddOpenApi();
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.AddHostedService<DatabaseSeederHostedService>();

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseOpenApiIfDev(app.Environment);
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
