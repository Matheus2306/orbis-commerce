
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orbis.Inventory.Api.Contracts;
using Orbis.Inventory.Api.Data;
using Orbis.Inventory.Api.Domain;

namespace Orbis.Inventory.Api.Endpoints;

public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock")
            .WithTags("Stock");

        group.MapPost("/", async (
            CreateStockRequest request,
            InventoryDbContext db,
            CancellationToken ct) =>
        {
            StockItem stock;

            try
            {
                stock = new StockItem(
                    request.ProductId,
                    request.Quantity);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(
                    new { error = ex.Message });
            }

            var exists = await db.StockItems
                .AnyAsync(
                    x => x.ProductId == request.ProductId,
                    ct);

            if (exists)
            {
                return Results.Conflict(
                    new { error = "Estoque já cadastrado." });
            }

            db.StockItems.Add(stock);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException pg
                    && pg.SqlState ==
                       PostgresErrorCodes.UniqueViolation
                    && pg.ConstraintName ==
                       "IX_stock_items_ProductId")
            {
                return Results.Conflict(
                    new { error = "Estoque já cadastrado." });
            }

            return Results.Created(
                $"/api/stock/{stock.ProductId}",
                stock);
        });

        group.MapGet("/{productId:guid}", async (
            Guid productId,
            InventoryDbContext db,
            CancellationToken ct) =>
        {
            var stock = await db.StockItems
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ProductId == productId,
                    ct);

            return stock is null
                ? Results.NotFound()
                : Results.Ok(stock);
        });

        group.MapGet("/", async (
            InventoryDbContext db,
            CancellationToken ct) =>
        {
            var stocks = await db.StockItems
                .AsNoTracking()
                .ToListAsync(ct);

            return Results.Ok(stocks);
        });

        return app;
    }
}
