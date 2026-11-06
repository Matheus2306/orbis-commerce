
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Orbis.Catalog.Api.Contracts;
using StackExchange.Redis;

namespace Orbis.Catalog.Api.Application;

public sealed class CachedProductReader
{
    private readonly IProductService _service;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedProductReader> _logger;

    public CachedProductReader(
        IProductService service,
        IDistributedCache cache,
        ILogger<CachedProductReader> logger)
    {
        _service = service;
        _cache = cache;
        _logger = logger;
    }

   public async Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(id);

        try
        {
            var cachedJson = await _cache.GetStringAsync(
                key,
                cancellationToken);

            if (cachedJson is not null)
            {
                var cachedProduct =
                    JsonSerializer.Deserialize<ProductResponse>(
                        cachedJson);

                if (cachedProduct is not null)
                {
                    _logger.LogInformation(
                        "Cache HIT: {ProductId}", id);

                    return cachedProduct;
                }
            }

            _logger.LogInformation(
                "Cache MISS: {ProductId}", id);
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis indisponivel na leitura: {ProductId}",
                id);
        }

        // PostgreSQL continua sendo a fonte de verdade.
        var product = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
            return null;

        try
        {
            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(product),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(5)
                },
                cancellationToken);
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis indisponivel na gravacao: {ProductId}",
                id);
        }

        return product;
    }

    public Task InvalidateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _cache.RemoveAsync(
            GetCacheKey(id),
            cancellationToken);
    }

    private static string GetCacheKey(Guid id)
    {
        return $"product:{id}";
    }

    private static bool IsCacheFailure(Exception ex)
    {
        return ex is RedisConnectionException
            or RedisTimeoutException
            or TimeoutException;
    }
}
