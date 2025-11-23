namespace UrlShortener.Shared.Domain;
public class ShortUrl
{
    public long Id { get; private set; }
    public string LongUrl { get; private set; } = null!;
    public string ShortCode { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public ShortUrl(
        long id, 
        string longUrl, 
        string shortUrl, 
        DateTime createdAt, 
        DateTime? expiresAt = null)
    {
        Id = id;
        LongUrl = longUrl ?? throw new ArgumentNullException(nameof(longUrl));
        ShortCode = shortUrl ?? throw new ArgumentNullException(nameof(shortUrl));
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    protected ShortUrl(){}
}
