using ApiGateway;
using Google.Protobuf.WellKnownTypes;
using ReadService.Grpc;
using WriteService.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["ShortUrlBase"] ??= "https://short.my";

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

app.UseCors("dev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

static bool IsValidShortCode(string? code)
{
    return !string.IsNullOrWhiteSpace(code);
}

// ------------ POST /api/shortUrls ------------

app.MapPost("/api/shortUrls", async (
    HttpRequest httpRequest,
    CreateShortUrlHttpRequest request,
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
    catch (Exception)
    {
        return Results.StatusCode(StatusCodes.Status502BadGateway);
    }

    if (string.IsNullOrWhiteSpace(grpcResponse.ShortUrl))
        return Results.StatusCode(StatusCodes.Status500InternalServerError);

    var shortBase = config["ShortUrlBase"]!.TrimEnd('/');
    var code = grpcResponse.ShortUrl;
    var shortUrl = $"{shortBase}/api/shortUrls/{code}";

    var httpResponse = new CreateShortUrlHttpResponse(
        Code: code,
        ShortUrl: shortUrl,
        LongUrl: request.LongUrl,
        Ttl: request.Ttl,
        CreatedAt: grpcResponse.CreatedAt.ToDateTime(),
        ExpiresAt: grpcResponse.ExpiresAt.ToDateTime()
    );

    return Results.Created(shortUrl, httpResponse);
});

static async Task<IResult> HandleRedirectAsync(
    string code,
    ReadService.Grpc.ReadService.ReadServiceClient readClient)
{
    if (!IsValidShortCode(code))
        return Results.BadRequest(new { error = "code is required" });

    var grpcRequest = new ReadServiceRequest
    {
        ShortUrl = code
    };

    ReadServiceResponse grpcResponse;
    try
    {
        grpcResponse = await readClient.GetShortUrlAsync(grpcRequest);
    }
    catch (Exception)
    {
        return Results.StatusCode(StatusCodes.Status502BadGateway);
    }

    if (string.IsNullOrWhiteSpace(grpcResponse.LongUrl))
        return Results.NotFound(new { error = "Short URL not found" });

    if (grpcResponse.IsExpired)
        return Results.StatusCode(StatusCodes.Status410Gone);
    
    return Results.Redirect(grpcResponse.LongUrl, permanent: false);
}

// ------------ GET /api/shortUrls/{code} ------------

app.MapGet("/api/shortUrls/{code}", (
    string code,
    ReadService.Grpc.ReadService.ReadServiceClient readClient) =>
    HandleRedirectAsync(code, readClient)
);

// ------------ GET /api/shortUrls/{code}/meta ------------

app.MapGet("/api/shortUrls/{code}/meta", async (
    string code,
    ReadService.Grpc.ReadService.ReadServiceClient readClient) =>
{
    if (!IsValidShortCode(code))
        return Results.BadRequest(new { error = "code is required" });

    var grpcRequest = new ReadServiceRequest
    {
        ShortUrl = code
    };

    ReadServiceResponse grpcResponse;
    try
    {
        grpcResponse = await readClient.GetShortUrlAsync(grpcRequest);
    }
    catch (Exception)
    {
        return Results.StatusCode(StatusCodes.Status502BadGateway);
    }

    if (string.IsNullOrWhiteSpace(grpcResponse.LongUrl))
        return Results.NotFound(new { error = "Short URL not found" });

    if (grpcResponse.IsExpired)
        return Results.StatusCode(StatusCodes.Status410Gone);

    var meta = new ShortUrlMetaHttpResponse(
        Code: code,
        LongUrl: grpcResponse.LongUrl,
        CreatedAt: grpcResponse.CreatedAt.ToDateTime(),
        ExpiresAt: grpcResponse.ExpiresAt.ToDateTime()
    );

    return Results.Ok(meta);
});

app.Run();
