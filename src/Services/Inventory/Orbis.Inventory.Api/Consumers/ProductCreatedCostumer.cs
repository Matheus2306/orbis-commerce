
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orbis.Contracts.Events;
using Orbis.Inventory.Api.Data;
using Orbis.Inventory.Api.Domain;

namespace Orbis.Inventory.Api.Consumers;

public sealed class ProductCreatedConsumer
    : IConsumer<ProductCreated>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(
        InventoryDbContext db,
        ILogger<ProductCreatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<ProductCreated> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "Recebido ProductCreated: {ProductId}",
            message.ProductId);

        var exists = await _db.StockItems
            .AnyAsync(
                x => x.ProductId == message.ProductId,
                ct);

        if (exists)
        {
            _logger.LogInformation(
                "Estoque já existente: {ProductId}",
                message.ProductId);

            return;
        }

        var stock = new StockItem(
            productId: message.ProductId,
            quantity: 0,
            reservedQuantity: 0
        );

        _db.StockItems.Add(stock);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (IsDuplicateStock(ex))
        {
            _db.Entry(stock).State = EntityState.Detached;

            _logger.LogInformation(
                "Estoque criado por processamento concorrente: {ProductId}",
                message.ProductId);

            return;
        }

        _logger.LogInformation(
            "Estoque inicial criado: {ProductId}",
            message.ProductId);
    }

    private static bool IsDuplicateStock(DbUpdateException ex)
    {
        return ex.InnerException is Npgsql.PostgresException pg
            && pg.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation
            && pg.ConstraintName == "IX_stock_items_ProductId";
    }
}
