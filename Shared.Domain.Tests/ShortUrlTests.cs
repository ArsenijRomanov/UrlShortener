using FluentAssertions;
using Xunit;
using UrlShortener.Shared.Domain;

namespace Shared.Domain.Tests;

public class ShortUrlTests
{
    [Fact]
    public void Ctor_Sets_Properties()
    {
        var longUrl = "https://example.com";
        var shortCode = "abcdef";
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(1);

        var entity = new ShortUrl(longUrl, shortCode, now, expiresAt);
        
        entity.LongUrl.Should().Be(longUrl);
        entity.ShortCode.Should().Be(shortCode);
        entity.CreatedAt.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(200));
        entity.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromMilliseconds(200));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_Invalid_LongUrl_Throws(string? longUrl)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(1);

        Action act = () => new ShortUrl(longUrl!, "shortCode", now, expiresAt);

        act.Should().Throw<ArgumentException>()
            .Where(x => x.ParamName == "longUrl");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_Invalid_ShortCode_Throws(string? shortCode)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(1);

        Action act = () => new ShortUrl("longUrl", shortCode!, now, expiresAt);

        act.Should().Throw<ArgumentException>()
            .Where(x => x.ParamName == "shortCode");
    }
}
