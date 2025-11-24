namespace UrlShortener.Shared.Domain.Exceptions;

public class UrlValidationException: Exception
{
    public UrlValidationException(string message) : base(message){}
}