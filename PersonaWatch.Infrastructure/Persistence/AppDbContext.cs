using Microsoft.EntityFrameworkCore;
using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<NewsContent> NewsContents => Set<NewsContent>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ReportsContent> ReportsContents => Set<ReportsContent>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
                entry.Entity.CreatedUserName = "system";
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedDate = DateTime.UtcNow;
                entry.Entity.UpdatedUserName = "system";
            }
        }

        return base.SaveChanges();
    }
}
