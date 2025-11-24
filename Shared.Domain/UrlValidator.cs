namespace UrlShortener.Shared.Domain.Exceptions;

public class UrlValidator
{
    private const int MaxUrlLength = 2048;

    public static void EnsureValidLongUrl(string? longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
            throw new UrlValidationException("LongUrl is required");
        
        if (longUrl.Length > MaxUrlLength)
            throw new UrlValidationException($"LongUrl is too long (> {MaxUrlLength} chars)");
        
        if (!Uri.TryCreate(longUrl, UriKind.Absolute, out var uri))
            throw new UrlValidationException("LongUrl has invalid format.");
        
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new UrlValidationException("Only http/https schemes are allowed.");

        if (string.IsNullOrEmpty(uri.Host))
            throw new UrlValidationException("LongUrl must contain host.");
    }
}
