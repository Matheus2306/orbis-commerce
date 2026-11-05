using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orbis.Catalog.Api.Contracts;
using Orbis.Catalog.Api.Data;
using Orbis.Catalog.Api.Domain;

namespace Orbis.Catalog.Api.Application;

public sealed class ProductService : IProductService
{
    private readonly CatalogDbContext _db;

    public ProductService(CatalogDbContext db)
    {
        _db = db;
    }    

    public async Task<CreateProductResult> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        Product product;
        try
        {
            product = new Product(
                request.Name,
                request.Sku, 
                request.Price
            );
        }
        catch(ArgumentException ex)
        {
            return new CreateProductResult(
                CreateProductStatus.Invalid,
                Error: ex.Message
            );
        }

        var exist = await _db.Products
            .AnyAsync(
                p => p.Sku == product.Sku,
                cancellationToken
            );

        if (exist)
        {
            return new CreateProductResult(
                CreateProductStatus.DuplicateSku,
                Error: "SKU já cadastrado."
            );
        }

        _db.Products.Add(product);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch(DbUpdateException ex) when (ex.InnerException is PostgresException
            {
                SqlState:PostgresErrorCodes.UniqueViolation
            })
        {
            return new CreateProductResult(
                CreateProductStatus.DuplicateSku,
                Error: "SKU já cadastrado."
            );
        }
        return new CreateProductResult(
            CreateProductStatus.Created,
            ProductResponse.FromEntity(product)
        );
    }

    public async Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == id,
                cancellationToken
            );

        return product is null
            ? null
            : ProductResponse.FromEntity(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Sku,
                p.Price,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => _db.Products.CountAsync(cancellationToken);
    
    public async Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        var produto = _db.Products.FirstOrDefault(p => p.Sku == sku);

        return produto != null ? true : false;
    }
}