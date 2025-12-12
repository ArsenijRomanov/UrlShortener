using System.Text.Json;
using StackExchange.Redis;

namespace Shared.Infrastructure;

public class RedisShortUrlCache(IConnectionMultiplexer connection) : IShortUrlCache
{
    private readonly IDatabase _db = connection.GetDatabase();

    private static string BuildKey(string shortCode) => $"short_url:{shortCode}";

    public async Task<CachedShortUrl?> GetAsync(string shortCode, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(BuildKey(shortCode));
        
        return !value.HasValue ? null : JsonSerializer.Deserialize<CachedShortUrl>(value.ToString());
    }

    public async Task SetAsync(string shortCode, CachedShortUrl model, TimeSpan ttl, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(model);

        await _db.StringSetAsync(
            key: BuildKey(shortCode), 
            value: json, 
            expiry: ttl);
    }
}
