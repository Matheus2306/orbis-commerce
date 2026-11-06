using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orbis.Catalog.Api.Application;
using Orbis.Catalog.Api.Contracts;

namespace Orbis.Catalog.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapPost("/", async (
            CreateProductRequest request,
            [FromServices] IProductService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(
                request,
                ct
            );

            return result.Status switch
            {
                CreateProductStatus.Created =>
                    Results.Created(
                        $"/api/products/{result.Product!.Id}",
                        result.Product
                    ),

                CreateProductStatus.DuplicateSku =>
                    Results.Conflict(
                        new { error = result.Error }
                    ),

                CreateProductStatus.Invalid =>
                    Results.BadRequest(
                        new { error = result.Error }
                    ),

                _ => Results.StatusCode(500)
            };
        });

        group.MapGet("/", async (
            [FromServices] IProductService service,
            CancellationToken ct) =>
        {
            var products = await service.GetAllAsync(ct);
            return Results.Ok(products);
        });

        group.MapGet("/count", async ( [FromServices] IProductService service ) =>
        {
            var total = await service.CountAsync();
            return Results.Ok(new { total });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            CachedProductReader reader,
            CancellationToken ct) =>
        {
            var product = await reader.GetByIdAsync(id, ct);

            return product is null
                ? Results.NotFound()
                : Results.Ok(product);
        });

        group.MapGet("sku/{sku}/exists", async (IProductService service, [FromQuery] string sku) =>
        {
            var exists = await service.ExistsBySkuAsync(sku);

            return Results.Ok(new { exists });
        });

        group.MapGet("id/{id}/exists", async (IProductService service, [FromQuery] Guid id) =>
        {
           var exists = await service.ExistsByIdAsync(id);
        });

        return app;
    }
}