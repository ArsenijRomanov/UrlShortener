namespace UrlShortener.Shared.Domain.Repositories;

public interface IShortUrlWriteRepository
{
    Task<ShortUrl> AddAsync(ShortUrl shortUrl, CancellationToken ct = default);
}
