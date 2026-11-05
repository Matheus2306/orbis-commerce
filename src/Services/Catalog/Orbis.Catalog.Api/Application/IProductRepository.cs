using System;
using Orbis.Catalog.Api.Domain;

namespace Orbis.Catalog.Api.Application;

public interface IProductRepository
{
    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        CancellationToken cancellationToken = default);
}
