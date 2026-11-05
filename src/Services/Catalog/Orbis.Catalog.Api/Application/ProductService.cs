using Orbis.Catalog.Api.Contracts;
using Orbis.Catalog.Api.Domain;

namespace Orbis.Catalog.Api.Application;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
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
                request.Price);
        }
        catch (ArgumentException ex)
        {
            return new CreateProductResult(
                CreateProductStatus.Invalid,
                Error: ex.Message);
        }

        var exists = await _repository.ExistsBySkuAsync(
            product.Sku,
            cancellationToken);

        if (exists)
        {
            return new CreateProductResult(
                CreateProductStatus.DuplicateSku,
                Error: "SKU já cadastrado.");
        }

        try
        {
            await _repository.AddAsync(
                product,
                cancellationToken);
        }
        catch (DuplicateProductSkuException)
        {
            return new CreateProductResult(
                CreateProductStatus.DuplicateSku,
                Error: "SKU já cadastrado.");
        }

        return new CreateProductResult(
            CreateProductStatus.Created,
            ProductResponse.FromEntity(product));
    }

    public async Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id);

        return product is null
            ? null
            : ProductResponse.FromEntity(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetAllAsync();
        var list = products.Select(p => new ProductResponse(
            p.Id, 
            p.Name, 
            p.Sku, 
            p.Price,
            p.CreatedAt
        )).ToList(); 
        return list;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        =>_repository.CountAsync();
    
    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _repository.ExistsBySkuAsync(sku);
    }
    public Task<bool> ExistsByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        return _repository.ExistsByIdAsync(id);
    }
}