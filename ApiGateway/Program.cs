using ApiGateway;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReadService.Grpc;
using WriteService.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["ShortUrlBase"] ??= "http://localhost:8080";

var writeServiceAddress = builder.Configuration["Grpc:WriteService"] ?? "http://localhost:6001";
var readServiceAddress  = builder.Configuration["Grpc:ReadService"]  ?? "http://localhost:6002";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGrpcClient<WriteService.Grpc.WriteService.WriteServiceClient>(o =>
{
    o.Address = new Uri(writeServiceAddress);
});

builder.Services.AddGrpcClient<ReadService.Grpc.ReadService.ReadServiceClient>(o =>
{
    o.Address = new Uri(readServiceAddress);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("dev");

// ---------------- helpers ----------------

static bool IsValidShortCode(string? code) =>
    !string.IsNullOrWhiteSpace(code);

static IResult NotFoundShortUrl() =>
    Results.NotFound(new { error = "Short URL not found" });

static IResult GoneShortUrl() =>
    Results.StatusCode(StatusCodes.Status410Gone);

static async Task<(IResult? Error, ReadServiceResponse? Data)> TryReadAsync(
    string code,
    ReadService.Grpc.ReadService.ReadServiceClient readClient)
{
    var grpcRequest = new ReadServiceRequest { ShortUrl = code };

    try
    {
        var resp = await readClient.GetShortUrlAsync(grpcRequest);
        return (null, resp);
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
    {
        return (NotFoundShortUrl(), null);
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
    {
        return (NotFoundShortUrl(), null);
    }
    catch (RpcException)
    {
        return (Results.StatusCode(StatusCodes.Status502BadGateway), null);
    }
    catch (Exception)
    {
        return (Results.StatusCode(StatusCodes.Status502BadGateway), null);
    }
}

// ---------------- POST /api/shortUrls ----------------

app.MapPost("/api/shortUrls", async (
    HttpRequest httpRequest,
    CreateShortUrlHttpRequest? request,
    WriteService.Grpc.WriteService.WriteServiceClient writeClient,
    IConfiguration config) =>
{
    if (httpRequest.ContentType is null ||
        !httpRequest.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Content-Type must be application/json" });
    }

    if (request is null)
        return Results.BadRequest(new { error = "Request body is required" });

    if (string.IsNullOrWhiteSpace(request.LongUrl))
        return Results.BadRequest(new { error = "longUrl is required" });

    var grpcRequest = new WriteServiceRequest
    {
        LongUrl = request.LongUrl,
        Ttl = request.Ttl ?? 0
    };

    WriteServiceResponse grpcResponse;
    try
    {
        grpcResponse = await writeClient.GetShortUrlAsync(grpcRequest);
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
    {
        return Results.BadRequest(new
        {
            code = "INVALID_LONG_URL",
            message = ex.Status.Detail
        });
    }
    catch (RpcException)
    {
        return Results.StatusCode(StatusCodes.Status502BadGateway);
    }
    catch (Exception)
    {
        return Results.StatusCode(StatusCodes.Status502BadGateway);
    }

    var code = grpcResponse.ShortUrl;
    if (string.IsNullOrWhiteSpace(code))
        return Results.StatusCode(StatusCodes.Status502BadGateway);

    var shortBase = (config["ShortUrlBase"] ?? "http://localhost:8080").TrimEnd('/');
    var shortUrl = $"{shortBase}/api/shortUrls/{code}";

    DateTime? expiresAt = grpcResponse.ExpiresAt is null
        ? null
        : grpcResponse.ExpiresAt.ToDateTime();

    var httpResponse = new CreateShortUrlHttpResponse(
        Code: code,
        ShortUrl: shortUrl,
        LongUrl: request.LongUrl,
        Ttl: request.Ttl,
        CreatedAt: grpcResponse.CreatedAt.ToDateTime(),
        ExpiresAt: expiresAt
    );

    return Results.Created(shortUrl, httpResponse);
});

// ---------------- GET /api/shortUrls/{code} ----------------

app.MapGet("/api/shortUrls/{code}", async (
    string code,
    ReadService.Grpc.ReadService.ReadServiceClient readClient) =>
{
    if (!IsValidShortCode(code))
        return NotFoundShortUrl();

    var (error, data) = await TryReadAsync(code, readClient);
    if (error is not null) return error;

    if (data is null || string.IsNullOrWhiteSpace(data.LongUrl))
        return NotFoundShortUrl();

    if (data.IsExpired)
        return GoneShortUrl();

    return Results.Redirect(data.LongUrl, permanent: false);
});

// ---------------- GET /api/shortUrls/{code}/meta ----------------

app.MapGet("/api/shortUrls/{code}/meta", async (
    string code,
    ReadService.Grpc.ReadService.ReadServiceClient readClient) =>
{
    if (!IsValidShortCode(code))
        return NotFoundShortUrl();

    var (error, data) = await TryReadAsync(code, readClient);
    if (error is not null) return error;

    if (data is null || string.IsNullOrWhiteSpace(data.LongUrl))
        return NotFoundShortUrl();

    if (data.IsExpired)
        return GoneShortUrl();

    DateTime? expiresAt = data.ExpiresAt is null
        ? null
        : data.ExpiresAt.ToDateTime();

    var meta = new ShortUrlMetaHttpResponse(
        Code: code,
        LongUrl: data.LongUrl,
        CreatedAt: data.CreatedAt.ToDateTime(),
        ExpiresAt: expiresAt
    );

    return Results.Ok(meta);
});

app.Run();
