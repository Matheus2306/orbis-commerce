
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orbis.Inventory.Api.Application;
using Orbis.Inventory.Api.Consumers;
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
                    request.Quantity,
                    request.ReservedQuantity);
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


        group.MapPost("/{productId:guid}/add", async (
            Guid productId,
            AddStockRequest request,
            InventoryDbContext db,
            CancellationToken ct) =>
        {
            if (request.Quantity <= 0)
                return Results.BadRequest(
                    new { error = "Quantidade inválida." });

            var affectedRows = await db.StockItems
                .Where(stock =>
                    stock.ProductId == productId &&
                    stock.Quantity <= int.MaxValue - request.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        stock => stock.Quantity,
                        stock => stock.Quantity + request.Quantity)
                    .SetProperty(
                        stock => stock.UpdatedAt,
                        DateTime.UtcNow),
                    ct);

            if (affectedRows == 0)
            {
                var exists = await db.StockItems
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.ProductId == productId,
                        ct);

                return exists
                    ? Results.Conflict(
                        new { error = "Limite de estoque excedido." })
                    : Results.NotFound(
                        new { error = "Estoque não encontrado." });
            }

            return Results.Ok(new
            {
                message = "Entrada de estoque registrada.",
                quantityAdded = request.Quantity
            });
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

        group.MapPost("/{productId}/reserve", async (
            Guid productId,
            ReservedStockRequest request,
            IStockReservationService service,
            CancellationToken ct) =>
        {
            var result = await service.ReserveAsync(
                productId,
                request.Quantity,
                ct);

            return result switch
            {
                ReservationStatus.Reserved => Results.Ok(new
                {
                    message = "Estoque reservado.",
                    productId,
                    quantity = request.Quantity
                }),
                ReservationStatus.insufficientStock => Results.Conflict(new
                {
                    error = "Estoque insuficiente."
                }),
                ReservationStatus.NotFound => Results.NotFound(new
                {
                    error = "Estoque não encontrado."
                }),
                ReservationStatus.invalidQuantity => Results.BadRequest(new
                {
                    error = "Quantidade inválida."
                }),

                _ => Results.StatusCode(500)
            };
        });


        return app;
    }
}
