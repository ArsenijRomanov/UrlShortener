using StackExchange.Redis;

namespace Shared.Infrastructure;


public class RedisShortUrlCache: IShortUrlCache
{
    private readonly IDatabase _db;

    public RedisShortUrlCache(IConnectionMultiplexer connection)
    {
        _db = connection.GetDatabase();
    }

    private string BuildKey(string shortCode) => $"shortCode: {shortCode}";
    
    public async Task<string?> GetLongUrlAsync(string shortCode, CancellationToken ct = default)
    {
        var longUrl = await _db.StringGetAsync(new RedisKey(BuildKey(shortCode)));
        if (!longUrl.HasValue) return null;
        return longUrl.ToString();
    }

    public async Task SetLongUrlAsync(string shortCode, string longUrl, TimeSpan ttl, CancellationToken ct = default)
    {
        await _db.StringSetAsync(BuildKey(shortCode), longUrl, ttl);
    }
}
