using Generator.Grpc;
using Grpc.Core;

namespace WriteService;

public class ShortCodeGeneratorClient: IShortCodeGenerator
{
    private readonly ShortCodeGeneratorService.ShortCodeGeneratorServiceClient _client;
    private readonly ILogger<ShortCodeGeneratorClient> _logger;

    public ShortCodeGeneratorClient(
        ShortCodeGeneratorService.ShortCodeGeneratorServiceClient client,
        ILogger<ShortCodeGeneratorClient> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        try
        {
            var shortCode = await _client.GenerateShortCodeAsync(
                new GenerateShortCodeRequest(), 
                cancellationToken: ct);
            
            if (string.IsNullOrWhiteSpace(shortCode.ShortCode))
            {
                _logger.LogError("Generator returned empty short code");
                throw new InvalidOperationException("Generator returned empty short code");
            }

            return shortCode.ShortCode;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while generating short code");
            throw;
        }
    }
}
