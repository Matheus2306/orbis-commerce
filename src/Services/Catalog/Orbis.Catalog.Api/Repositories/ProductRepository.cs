
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orbis.Catalog.Api.Application;
using Orbis.Catalog.Api.Data;
using Orbis.Catalog.Api.Domain;

namespace Orbis.Catalog.Api.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _db;

    public ProductRepository(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        _db.Products.Add(product);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg
                  && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _db.Entry(product).State = EntityState.Detached;

            throw new DuplicateProductSkuException(
                product.Sku, ex);
        }
    }

    public Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        return _db.Products.AnyAsync(
            p => p.Sku == sku,
            cancellationToken);
    }

    public Task<bool> ExistsByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _db.Products.AnyAsync(
            p => p.Id == id,
            cancellationToken);
    }

    public Task<int> CountAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.Products.CountAsync(cancellationToken);
    }
}
