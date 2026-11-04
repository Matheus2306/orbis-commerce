using Microsoft.EntityFrameworkCore;
using Orbis.Catalog.Api.Contracts;
using Orbis.Catalog.Api.Data;
using Orbis.Catalog.Api.Domain;

namespace Orbis.Catalog.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapPost("/", async (
            CreateProductRequest request,
            CatalogDbContext db,
            CancellationToken ct) =>
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
            catch (ArgumentException ex)
            {
                return Results.BadRequest(
                    new { error = ex.Message });
            }

            var skuExists = await db.Products
                .AnyAsync(p => p.Sku == product.Sku, ct);

            if (skuExists)
            {
                return Results.Conflict(
                    new { error = "SKU já cadastrado." });
            }

            db.Products.Add(product);

            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/products/{product.Id}",
                ProductResponse.FromEntity(product)
            );
        });

        group.MapGet("/", async (
            CatalogDbContext db,
            CancellationToken ct) =>
        {
            var products = await db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.Name,
                    p.Sku,
                    p.Price,
                    p.CreatedAt
                ))
                .ToListAsync(ct);

            return Results.Ok(products);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            CatalogDbContext db,
            CancellationToken ct) =>
        {
            var product = await db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            return product is null
                ? Results.NotFound()
                : Results.Ok(
                    ProductResponse.FromEntity(product));
        });

        return app;
    }
}