namespace ApiGateway; 

public record CreateShortUrlHttpRequest(string LongUrl, long? Ttl);

public record CreateShortUrlHttpResponse(
    string Code,
    string ShortUrl,
    string LongUrl,
    long? Ttl,
    DateTime CreatedAt,
    DateTime ExpiresAt
);

public record ShortUrlMetaHttpResponse(
    string Code,
    string LongUrl,
    DateTime CreatedAt,
    DateTime ExpiresAt
);