namespace UrlShortener.Shared.Domain;

public interface IShortUrlWriteRepository
{
    Task<ShortUrl> AddAsync(ShortUrl shortUrl, CancellationToken ct = default);
}
