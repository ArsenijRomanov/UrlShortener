using Generator.Grpc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Shared.Infrastructure;
using SharpJuice.Essentials;
using UrlShortener.Shared.Domain.Repositories;
using WriteService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddDbContext<ShortUrlDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("WriteDb")
                     ?? throw new Exception("ConnectionStrings:WriteDb is not configured");
    options.UseNpgsql(connString);
});

builder.Services.AddScoped<IShortUrlWriteRepository, ShortUrlRepository>();

builder.Services.AddScoped<IShortCodeGenerator, ShortCodeGeneratorClient>();

builder.Services.AddGrpcClient<ShortCodeGeneratorService.ShortCodeGeneratorServiceClient>(o =>
{
    var generatorConnectionString = 
        builder.Configuration.GetConnectionString("Generator")
        ?? throw new Exception("ConnectionStrings:Generator is not configured");
    o.Address = new Uri(generatorConnectionString); 
});

var app = builder.Build();

app.MapGrpcService<WriteServiceGrpc>();

app.MapGet("/", () => "Write gRPC service. Use a gRPC client.");

app.Run();
