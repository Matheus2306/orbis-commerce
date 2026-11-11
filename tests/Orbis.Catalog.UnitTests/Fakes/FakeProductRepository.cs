
using Orbis.Catalog.Api.Application;
using Orbis.Catalog.Api.Domain;

namespace Orbis.Catalog.UnitTests.Fakes;

public sealed class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _products = new();

    public int SaveCalls { get; private set; }

    public Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        if (_products.Any(p => p.Sku == product.Sku))
        {
            throw new DuplicateProductSkuException(product.Sku);
        }

        _products.Add(product);


        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        return Task.FromResult(1);
    }

    public Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);

        return Task.FromResult(product);
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Product> result = _products.ToList();

        return Task.FromResult(result);
    }

    public Task<bool> ExistsBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _products.Any(p => p.Sku == sku));
    }

    public Task<bool> ExistsByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _products.Any(p => p.Id == id));
    }

    public Task<int> CountAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_products.Count);
    }
}
