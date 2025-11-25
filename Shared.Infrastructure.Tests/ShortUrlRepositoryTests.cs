using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Shared.Domain;
using UrlShortener.Shared.Infrastructure;
using Xunit;

namespace Shared.Infrastructure.Tests;

public class ShortUrlRepositoryTests
{
    private ShortUrlDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShortUrlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ShortUrlDbContext(options);
    }
    
    private async Task<ShortUrlDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ShortUrlDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ShortUrlDbContext(options);
        
        await context.Database.EnsureCreatedAsync();

        return context;
    }

    [Fact]
    public async Task AddAsync_Persists_Entity_With_All_Fields()
    {
        await using var db = CreateContext();
        var repo = new ShortUrlRepository(db);

        var now = new DateTime(2025, 1, 1, 12, 0, 0);
        
        var entity = new ShortUrl(
            "longUrl", 
            "shortCode", 
            now, 
            now.AddDays(1));

        await repo.AddAsync(entity);

        var entityFromDb = await db.ShortUrls.SingleAsync();
        
        entityFromDb.LongUrl.Should().Be(entity.LongUrl);
        entityFromDb.ShortCode.Should().Be(entity.ShortCode);
        entityFromDb.CreatedAt.Should().Be(entity.CreatedAt);
        entityFromDb.ExpiresAt.Should().Be(entity.ExpiresAt!.Value);
    }
    
    [Fact]
    public async Task AddAsync_With_Null_Expires_At()
    {
        await using var db = CreateContext();
        var repo = new ShortUrlRepository(db);

        var now = new DateTime(2025, 1, 1, 12, 0, 0);
        
        var entity = new ShortUrl(
            "longUrl", 
            "shortCode", 
            now);

        await repo.AddAsync(entity);

        var entityFromDb = await db.ShortUrls.SingleAsync();
        
        entityFromDb.LongUrl.Should().Be(entity.LongUrl);
        entityFromDb.ShortCode.Should().Be(entity.ShortCode);
        entityFromDb.CreatedAt.Should().Be(entity.CreatedAt);
        Assert.Null(entityFromDb.ExpiresAt);
    }

    [Fact]
    public async Task GetByShortCodeAsync_Returns_Entity_With_All_Fields()
    {
        await using var db = CreateContext();
        var repo = new ShortUrlRepository(db);

        var now = new DateTime(2025, 1, 1, 12, 0, 0);
        
        var entity = new ShortUrl(
            "longUrl", 
            "shortCode", 
            now, 
            now.AddDays(1));

        await db.ShortUrls.AddAsync(entity);
        await db.SaveChangesAsync();

        var entityFromRepo = await repo.GetByShortCodeAsync(entity.ShortCode);

        Assert.NotNull(entityFromRepo);
        entityFromRepo.LongUrl.Should().Be(entity.LongUrl);
        entityFromRepo.ShortCode.Should().Be(entity.ShortCode);
        entityFromRepo.CreatedAt.Should().Be(entity.CreatedAt);
        entityFromRepo.ExpiresAt.Should().Be(entity.ExpiresAt!.Value);
    }
    
    [Fact]
    public async Task GetByShortCodeAsync_Returns_Entity_With_Null_ExpiresAt()
    {
        await using var db = CreateContext();
        var repo = new ShortUrlRepository(db);

        var now = new DateTime(2025, 1, 1, 12, 0, 0);
        
        var entity = new ShortUrl(
            "longUrl", 
            "shortCode", 
            now);

        await db.ShortUrls.AddAsync(entity);
        await db.SaveChangesAsync();

        var entityFromRepo = await repo.GetByShortCodeAsync(entity.ShortCode);

        Assert.NotNull(entityFromRepo);
        entityFromRepo.LongUrl.Should().Be(entity.LongUrl);
        entityFromRepo.ShortCode.Should().Be(entity.ShortCode);
        entityFromRepo.CreatedAt.Should().Be(entity.CreatedAt);
        Assert.Null(entityFromRepo.ExpiresAt);
    }

    [Fact]
    public async Task GetByShortCodeAsync_After_AddAsync()
    {
        await using var db = CreateContext();
        var repo = new ShortUrlRepository(db);

        var now = new DateTime(2025, 1, 1, 12, 0, 0);
        
        var entity = new ShortUrl(
            "longUrl", 
            "shortCode", 
            now,
            now.AddDays(1));

        await repo.AddAsync(entity);

        var entityFromRepo = await repo.GetByShortCodeAsync(entity.ShortCode);
        
        Assert.NotNull(entityFromRepo);
        entityFromRepo.LongUrl.Should().Be(entity.LongUrl);
        entityFromRepo.ShortCode.Should().Be(entity.ShortCode);
        entityFromRepo.CreatedAt.Should().Be(entity.CreatedAt);
        entityFromRepo.ExpiresAt.Should().Be(entity.ExpiresAt!.Value);
    }

    [Fact]
    public async Task GetByShortCodeAsync_Returns_Null_When_Not_Found()
    {
        await using var db = CreateContext();
        var repo = new ShortUrlRepository(db);
        
        var entityFromRepo = await repo.GetByShortCodeAsync("Non-existent_shortCode");

        entityFromRepo.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_Throws_When_ShortCode_Not_Unique()
    {
        await using var db = await CreateSqliteContextAsync();
        var repo = new ShortUrlRepository(db);

        var now = new DateTime(2025, 1, 1, 12, 0, 0);

        var firstEntity = new ShortUrl(
            "firstLongUrl",
            "shortCode",
            now,
            now.AddDays(1));

        var secondEntity = new ShortUrl(
            "secondLongUrl",
            "shortCode",
            now.AddDays(2),
            now.AddDays(3));

        await repo.AddAsync(firstEntity);

        Func<Task> act = () => repo.AddAsync(secondEntity);

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
