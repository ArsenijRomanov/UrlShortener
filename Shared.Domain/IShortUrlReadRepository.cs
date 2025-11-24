namespace UrlShortener.Shared.Domain.Repositories;

public interface IShortUrlReadRepository
{
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default);
}
