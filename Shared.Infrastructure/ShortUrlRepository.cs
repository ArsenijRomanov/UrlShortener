using Microsoft.EntityFrameworkCore;
using UrlShortener.Shared.Domain;
using UrlShortener.Shared.Domain.Repositories;

namespace UrlShortener.Shared.Infrastructure;

public class ShortUrlRepository: IShortUrlReadRepository, IShortUrlWriteRepository
{
    private readonly ShortUrlDbContext _db;

    public ShortUrlRepository(ShortUrlDbContext db)
    {
        _db = db;
    }
    
    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default)
    {
        return await _db.ShortUrls
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShortCode == shortCode, ct);
    }

    public async Task<ShortUrl> AddAsync(ShortUrl shortUrl, CancellationToken ct = default)
    {
        await _db.ShortUrls.AddAsync(shortUrl, ct);
        await _db.SaveChangesAsync(ct);
        return shortUrl;
    }
}
