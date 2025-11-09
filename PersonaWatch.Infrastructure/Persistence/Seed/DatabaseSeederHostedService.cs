using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonaWatch.Domain.Entities;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Infrastructure.Persistence.Seed;

public class DatabaseSeederHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    public DatabaseSeederHostedService(IServiceProvider sp) => _sp = sp;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // İsteğe bağlı: migration
        // await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Users.AnyAsync(u => u.Username == "admin", cancellationToken))
        {
            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Username = "admin",
                FirstName = "Admin",
                LastName  = "Admin",
                IsAdmin   = true
            };
            user.Password = hasher.HashPassword(user, "admin");
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
