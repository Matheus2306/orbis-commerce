using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orbis.Catalog.Api.Application;
using Orbis.Catalog.Api.Contracts;
using StackExchange.Redis;

namespace Orbis.Catalog.UnitTests.Application;

public sealed class CachedProductReaderTests
{
    private readonly Mock<IProductService> _service = new(MockBehavior.Strict);
    private readonly Mock<IDistributedCache> _cache = new(MockBehavior.Strict);
    private readonly ProductResponse _product = new(Guid.NewGuid(), "Product", "SKU", 10, DateTime.UtcNow);

    private CachedProductReader Reader() => new(
        _service.Object, _cache.Object, NullLogger<CachedProductReader>.Instance);

    [Fact]
    public async Task Get_CacheHit_DoesNotQueryService()
    {
        _cache.Setup(x => x.GetAsync($"product:{_product.Id}", default))
            .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(_product));
        Assert.Equal(_product, await Reader().GetByIdAsync(_product.Id));
        _service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    public async Task Get_CacheMiss_LoadsAndCachesForFiveMinutes(string? cached)
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var key = $"product:{_product.Id}";
        _cache.Setup(x => x.GetAsync(key, token))
            .ReturnsAsync(cached is null ? null : Encoding.UTF8.GetBytes(cached));
        _service.Setup(x => x.GetByIdAsync(_product.Id, token)).ReturnsAsync(_product);
        _cache.Setup(x => x.SetAsync(key, It.Is<byte[]>(b =>
                JsonSerializer.Deserialize<ProductResponse>(b, (JsonSerializerOptions?)null) == _product),
            It.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)), token))
            .Returns(Task.CompletedTask);

        Assert.Equal(_product, await Reader().GetByIdAsync(_product.Id, token));
        _service.VerifyAll();
        _cache.VerifyAll();
    }

    [Fact]
    public async Task Get_MissingProduct_DoesNotCache()
    {
        _cache.Setup(x => x.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
        _service.Setup(x => x.GetByIdAsync(_product.Id, default)).ReturnsAsync((ProductResponse?)null);
        Assert.Null(await Reader().GetByIdAsync(_product.Id));
        _cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public static TheoryData<Exception> CacheFailures => new()
    {
        new TimeoutException("timeout"),
        new RedisTimeoutException("timeout", CommandStatus.Unknown),
        new RedisConnectionException(ConnectionFailureType.UnableToConnect, "offline")
    };

    [Theory]
    [MemberData(nameof(CacheFailures))]
    public async Task Get_CacheReadAndWriteUnavailable_ReturnsSourceProduct(Exception error)
    {
        _cache.Setup(x => x.GetAsync(It.IsAny<string>(), default)).ThrowsAsync(error);
        _service.Setup(x => x.GetByIdAsync(_product.Id, default)).ReturnsAsync(_product);
        _cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), default)).ThrowsAsync(error);

        Assert.Equal(_product, await Reader().GetByIdAsync(_product.Id));
        _service.VerifyAll();
        _cache.VerifyAll();
    }

    [Fact]
    public async Task Get_CanceledRead_PropagatesCancellation()
    {
        _cache.Setup(x => x.GetAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new OperationCanceledException());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Reader().GetByIdAsync(_product.Id));
        _service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Invalidate_RemovesOnlyRequestedKey()
    {
        using var cts = new CancellationTokenSource();
        _cache.Setup(x => x.RemoveAsync($"product:{_product.Id}", cts.Token)).Returns(Task.CompletedTask);
        await Reader().InvalidateAsync(_product.Id, cts.Token);
        _cache.Verify(x => x.RemoveAsync($"product:{_product.Id}", cts.Token), Times.Once);
        _cache.VerifyNoOtherCalls();
        _service.VerifyNoOtherCalls();
    }
}
