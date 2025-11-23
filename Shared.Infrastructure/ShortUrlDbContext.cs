using Microsoft.EntityFrameworkCore;
using UrlShortener.Shared.Domain;

namespace UrlShortener.Shared.Infrastructure;

public class ShortUrlDbContext: DbContext
{
    public DbSet<ShortUrl> ShortUrls { get; set; } = null!;
    
    public ShortUrlDbContext(DbContextOptions<ShortUrlDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ShortUrlConfiguration());
    }
}
