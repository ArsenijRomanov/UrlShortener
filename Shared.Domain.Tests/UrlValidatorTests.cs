using FluentAssertions;
using UrlShortener.Shared.Domain.Exceptions;
using Xunit;

namespace Shared.Domain.Tests;

public class UrlValidatorTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path/to/resource?x=1&y=2")]
    [InlineData("https://sub.domain.example.com:8080/path#fragment")]
    public void EnsureValidLongUrl_ValidUrls_DoesNotThrow(string url)
    {
        Action act = () => UrlValidator.EnsureValidLongUrl(url);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void EnsureValidLongUrl_NullOrWhitespace_Throws(string? url)
    {
        Action act = () => UrlValidator.EnsureValidLongUrl(url);

        act.Should()
            .Throw<UrlValidationException>()
            .Where(e => e.Message.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnsureValidLongUrl_TooLong_Throws()
    {
        var longPart = new string('a', 2050);
        var url = $"https://{longPart}.com";

        Action act = () => UrlValidator.EnsureValidLongUrl(url);

        act.Should()
            .Throw<UrlValidationException>()
            .Where(e => e.Message.Contains("too long", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("://bad-url")]
    [InlineData("not a url")]
    public void EnsureValidLongUrl_InvalidFormat_Throws(string url)
    {
        Action act = () => UrlValidator.EnsureValidLongUrl(url);

        act.Should()
            .Throw<UrlValidationException>()
            .Where(e => e.Message.Contains("invalid format", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("mailto:test@example.com")]
    [InlineData("ws://example.com")]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    public void EnsureValidLongUrl_InvalidScheme_Throws(string url)
    {
        Action act = () => UrlValidator.EnsureValidLongUrl(url);

        act.Should().Throw<UrlValidationException>();
    }
}
