using ApiGateway;
using Google.Protobuf.WellKnownTypes;
using ReadService.Grpc;
using WriteService.Grpc;

var builder = WebApplication.CreateBuilder(args);

// База коротких ссылок (используется при формировании shortUrl в ответе)
builder.Configuration["ShortUrlBase"] ??= "https://short.my";

// Адреса gRPC-сервисов
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ------------ вспомогательные методы ------------

static bool IsValidShortCode(string? code)
{
    // Базовая техническая валидация идентификатора:
    // - непустой
    // Остальные проверки (алфавит base62, длина и т.п.) считаем доменной логикой ReadService.
    return !string.IsNullOrWhiteSpace(code);
}

// ------------ POST /api/shortUrls ------------
// Создание короткой ссылки по Long URL

app.MapPost("/api/shortUrls", async (
    HttpRequest httpRequest,
    CreateShortUrlHttpRequest request,
    WriteService.Grpc.WriteService.WriteServiceClient writeClient,
    IConfiguration config) =>
{
    // 1. Техническая проверка Content-Type
    if (httpRequest.ContentType is null ||
        !httpRequest.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Content-Type must be application/json" });
    }

    // 2. Техническая проверка: тело пришло и было распарсено как JSON
    if (request is null)
        return Results.BadRequest(new { error = "Request body is required" });

    // 3. Техническая проверка: обязательное поле longUrl присутствует и не пустое
    // (без проверки формата/схемы – это уже доменная валидация, которая должна быть в WriteService)
    if (string.IsNullOrWhiteSpace(request.LongUrl))
        return Results.BadRequest(new { error = "longUrl is required" });

    // Типы полей (string для longUrl, int64 для ttl) обеспечивает модель-биндинг/JSON десериализация.
    // Никаких проверок Uri и диапазонов TTL тут не делаем – это задача WriteService.

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
        // Доменные/технические ошибки write-сервиса мы проксируем как 502
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

// Общий handler для редиректа, чтобы переиспользовать его на разных маршрутах
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

    // 302 Found (temporary) – соответствует тех.решению
    return Results.Redirect(grpcResponse.LongUrl, permanent: false);
}

// ------------ GET /api/shortUrls/{code} ------------
// API-эндпоинт редиректа

app.MapGet("/api/shortUrls/{code}", (
    string code,
    ReadService.Grpc.ReadService.ReadServiceClient readClient) =>
    HandleRedirectAsync(code, readClient)
);

// ------------ GET /api/shortUrls/{code}/meta ------------
// Получение метаданных по Short URL

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
