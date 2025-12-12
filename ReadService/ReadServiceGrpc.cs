using UrlShortener.Shared.Domain.Repositories;
using SharpJuice.Essentials;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReadService.Grpc;
using Shared.Infrastructure;

namespace ReadService;

public class ReadServiceGrpc(
    IClock clock,
    IShortUrlReadRepository readRepo,
    IShortUrlCache cache,
    ILogger<ReadServiceGrpc> logger)
    : Grpc.ReadService.ReadServiceBase
{
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(1);

    public override async Task<ReadServiceResponse> GetShortUrl(ReadServiceRequest request, ServerCallContext context)
    {
        try
        {
            CachedShortUrl? cachedData = null;
            try 
            {
                cachedData = await cache.GetAsync(request.ShortUrl, context.CancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Redis unavailable for {ShortUrl}", request.ShortUrl);
            }

            if (cachedData != null)
            {
                return MapToResponse(cachedData);
            }

            var entity = await readRepo.GetByShortCodeAsync(
                request.ShortUrl, 
                context.CancellationToken);

            if (entity == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Short code not found"));

            var cacheModel = new CachedShortUrl(entity.LongUrl, entity.CreatedAt, entity.ExpiresAt);

            try
            {
                 await cache.SetAsync(request.ShortUrl, cacheModel, _cacheTtl, context.CancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write to cache for {ShortUrl}", request.ShortUrl);
            }

            return MapToResponse(cacheModel);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Internal error in ReadService for {ShortUrl}", request.ShortUrl);
            throw new RpcException(new Status(StatusCode.Internal, "Internal error"));
        }
    }

    private ReadServiceResponse MapToResponse(CachedShortUrl data)
    {
        var nowUtc = clock.Now.UtcDateTime;
        
        return new ReadServiceResponse
        {
            LongUrl = data.LongUrl,
            CreatedAt = Timestamp.FromDateTime(data.CreatedAt.ToUniversalTime()),
            ExpiresAt = data.ExpiresAt.HasValue 
                ? Timestamp.FromDateTime(data.ExpiresAt.Value.ToUniversalTime()) 
                : null,
            IsExpired = data.ExpiresAt.HasValue && (data.ExpiresAt.Value < nowUtc)
        };
    }
}
