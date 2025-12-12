namespace UrlShortener.Shared.Infrastructure;

using SharpJuice.Essentials;

public class SystemClock: IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
