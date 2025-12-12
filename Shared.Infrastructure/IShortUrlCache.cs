namespace Shared.Infrastructure;

public record CachedShortUrl(string LongUrl, DateTime CreatedAt, DateTime? ExpiresAt);

public interface IShortUrlCache
{
    Task<CachedShortUrl?> GetAsync(string shortCode, CancellationToken ct = default);
    
    Task SetAsync(string shortCode, CachedShortUrl model, TimeSpan ttl, CancellationToken ct = default);
}
