namespace UrlShortener.Shared.Domain;
public class ShortUrl
{
    public long Id { get; private set; }
    public string LongUrl { get; private set; } = null!;
    public string ShortCode { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public ShortUrl(
        string longUrl, 
        string shortCode, 
        DateTime createdAt, 
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
            throw new ArgumentException("LongUrl must not be empty or whitespace", nameof(longUrl));

        if (string.IsNullOrWhiteSpace(shortCode))
            throw new ArgumentException("ShortCode must not be empty or whitespace", nameof(shortCode));

        LongUrl = longUrl;
        ShortCode = shortCode;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    protected ShortUrl(){}
}
