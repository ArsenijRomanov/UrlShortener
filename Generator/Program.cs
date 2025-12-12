using Generator;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using UrlShortener.Shared.Infrastructure;
using SharpJuice.Essentials;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
builder.Services.AddSingleton<IClock, SystemClock>();

var instanceId = builder.Configuration.GetValue<ushort>("Snowflake:InstanceId");
builder.Services.AddSingleton(new SnowFlakeGeneratorConfig(instanceId));
builder.Services.AddSingleton<IGenerator, SnowFlakeGenerator>();
builder.Services.AddSingleton<ShortCodeGeneratorGrpcService>();

var app = builder.Build();

app.MapGrpcService<ShortCodeGeneratorGrpcService>();
app.MapGet("/", () => "Short code generator gRPC service. Use a gRPC client.");

app.Run();
