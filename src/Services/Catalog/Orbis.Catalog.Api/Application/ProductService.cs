using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orbis.Catalog.Api.Contracts;
using Orbis.Catalog.Api.Domain;
using Orbis.Contracts.Events;

namespace Orbis.Catalog.Api.Application;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IPublishEndpoint _publisher;

    public ProductService(IProductRepository repository, IPublishEndpoint publisher)
    {
        _repository = repository;
        _publisher = publisher;
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

    if (await _repository.ExistsBySkuAsync(
        product.Sku, cancellationToken))
    {
        return new CreateProductResult(
            CreateProductStatus.DuplicateSku,
            Error: "SKU já cadastrado.");
    }

    var productCreated = new ProductCreated(
        EventId: Guid.NewGuid(),
        ProductId: product.Id,
        Sku: product.Sku,
        OccurredAt: DateTime.UtcNow
    );

    // Prepara a entidade para persistencia.
    await _repository.AddAsync(
        product,
        cancellationToken);

    // Registra o evento no Bus Outbox.
    await _publisher.Publish(
        productCreated,
        cancellationToken);

    try
    {
        // Confirma produto e mensagem juntos.
        await _repository.SaveChangesAsync(
            cancellationToken);
    }
    catch (DbUpdateException ex) when (
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName == "IX_products_Sku")
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