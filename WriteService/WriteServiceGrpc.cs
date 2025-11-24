using UrlShortener.Shared.Domain.Repositories;
using SharpJuice.Essentials;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UrlShortener.Shared.Domain;
using WriteService.Grpc;
using UrlShortener.Shared.Domain.Exceptions;

namespace WriteService;

public class WriteServiceGrpc : Grpc.WriteService.WriteServiceBase
{
    private readonly IShortUrlWriteRepository _writeRepo;
    private readonly IClock _clock;
    private readonly ILogger<WriteServiceGrpc> _logger;
    private readonly IShortCodeGenerator _shortCodeGenerator;

    public WriteServiceGrpc(
        IShortUrlWriteRepository writeRepo,
        IClock clock,
        ILogger<WriteServiceGrpc> logger,
        IShortCodeGenerator generator
    )
    {
        _writeRepo = writeRepo;
        _clock = clock;
        _logger = logger;
        _shortCodeGenerator = generator;
    }

    public override async Task<WriteServiceResponse> GetShortUrl(WriteServiceRequest request, ServerCallContext context)
    {
        try
        {
            UrlValidator.EnsureValidLongUrl(request.LongUrl);

            if (request.Ttl < 0)
                throw new UrlValidationException("TTL must be >= 0");
            
            var now = _clock.Now.UtcDateTime;
            DateTime? expiresAt = null;
            
            if (request.Ttl > 0)
                expiresAt = now.AddSeconds(request.Ttl);

            var shortCode = await _shortCodeGenerator.GenerateAsync(context.CancellationToken);

            var shortUrl = new ShortUrl(request.LongUrl, shortCode, now, expiresAt);

            await _writeRepo.AddAsync(shortUrl, context.CancellationToken);

            return new WriteServiceResponse
            {
                ShortUrl = shortUrl.ShortCode,
                CreatedAt = Timestamp.FromDateTime(shortUrl.CreatedAt.ToUniversalTime()),
                ExpiresAt = shortUrl.ExpiresAt.HasValue
                    ? Timestamp.FromDateTime(shortUrl.ExpiresAt.Value.ToUniversalTime())
                    : null
            };
        }
        catch (UrlValidationException e)
        {
            _logger.LogWarning(e, "URL validation failed for {LongUrl}", request.LongUrl);
            throw new RpcException(new Status(StatusCode.InvalidArgument, e.Message));
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
            _logger.LogError(e, "Unexpected error while creating short url");
            throw new RpcException(new Status(StatusCode.Internal, "Internal error while creating short url"));
        }
    }
}
