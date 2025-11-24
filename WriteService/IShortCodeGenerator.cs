namespace WriteService;

public interface IShortCodeGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
