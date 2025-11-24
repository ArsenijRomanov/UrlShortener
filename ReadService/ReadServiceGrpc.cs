using UrlShortener.Shared.Domain.Repositories;
using SharpJuice.Essentials;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReadService.Grpc;

namespace ReadService;

public class ReadServiceGrpc : Grpc.ReadService.ReadServiceBase
{
    private readonly IClock _clock;
    private readonly IShortUrlReadRepository _readRepo;
    private readonly ILogger<ReadServiceGrpc> _logger;

    public ReadServiceGrpc(
        IClock clock,
        IShortUrlReadRepository readRepo,
        ILogger<ReadServiceGrpc> logger)
    {
        _clock = clock;
        _readRepo = readRepo;
        _logger = logger;
    }
    public override async Task<ReadServiceResponse> GetShortUrl(ReadServiceRequest request, ServerCallContext context)
    {
        try
        {
            var shortUrl = await _readRepo.GetByShortCodeAsync(
                request.ShortUrl, 
                context.CancellationToken);

            if (shortUrl == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Short code not found"));
            
            return new ReadServiceResponse
            {
                LongUrl = shortUrl.LongUrl,
                CreatedAt = Timestamp.FromDateTime(shortUrl.CreatedAt.ToUniversalTime()),
                ExpiresAt = shortUrl.ExpiresAt.HasValue 
                    ? Timestamp.FromDateTime(shortUrl.ExpiresAt.Value.ToUniversalTime()) : null,
                IsExpired = shortUrl.ExpiresAt.HasValue && (shortUrl.ExpiresAt.Value < _clock.Now.UtcDateTime)
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error while reading short url {ShortUrl}", request.ShortUrl);
            throw new RpcException(new Status(StatusCode.Internal, "Internal error while reading short url"));
        }
    }
}
