namespace UrlShortener.Shared.Domain;

public interface IShortUrlReadRepository
{
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default);
}
