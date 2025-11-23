namespace Shared.Infrastructure;

public interface IShortUrlCache
{
    Task<string?> GetLongUrlAsync(
        string shortCode, 
        CancellationToken ct = default);
    
    Task SetLongUrlAsync(
        string shortCode, 
        string longUrl, 
        TimeSpan ttl, 
        CancellationToken ct = default);
}
