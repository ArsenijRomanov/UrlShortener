using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using ReadService;
using Shared.Infrastructure;
using SharpJuice.Essentials;
using StackExchange.Redis;
using UrlShortener.Shared.Domain.Repositories;
using UrlShortener.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddDbContext<ShortUrlDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("ReadDb")
                     ?? throw new Exception("ConnectionStrings:ReadDb is not configured");
    options.UseNpgsql(connString);
});
builder.Services.AddScoped<IShortUrlReadRepository, ShortUrlRepository>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConn = builder.Configuration.GetConnectionString("Redis") ?? throw new NullReferenceException();
    return ConnectionMultiplexer.Connect(redisConn);
});
builder.Services.AddSingleton<IShortUrlCache, RedisShortUrlCache>();

var app = builder.Build();

app.MapGrpcService<ReadServiceGrpc>();
app.MapGet("/", () => "Read gRPC service. Use a gRPC client.");

app.Run();
