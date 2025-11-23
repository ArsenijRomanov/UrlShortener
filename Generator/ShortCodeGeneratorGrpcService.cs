using Grpc.Core;
using Generator.Grpc;

namespace Generator;

public class ShortCodeGeneratorGrpcService: ShortCodeGeneratorService.ShortCodeGeneratorServiceBase
{
    private readonly IGenerator _generator;
    private readonly ILogger<ShortCodeGeneratorGrpcService> _logger;

    public ShortCodeGeneratorGrpcService(IGenerator generator, ILogger<ShortCodeGeneratorGrpcService> logger)
    {
        _generator = generator;
        _logger = logger;
    }

    public override async Task<GenerateShortCodeResponse> GenerateShortCode(
        GenerateShortCodeRequest request, 
        ServerCallContext context)
    {
        try
        {
            var code = _generator.GenerateShortCode();
            return new GenerateShortCodeResponse { ShortCode = code };
        }
        catch (RpcException) 
        {
            throw;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Short code generation failed");
            throw new RpcException(new Status(StatusCode.Internal, "Short code generation exception"));
        }
    }
}
