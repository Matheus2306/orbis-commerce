
using Microsoft.EntityFrameworkCore;
using Orbis.Inventory.Api.Data;

namespace Orbis.Inventory.Api.Application;

public sealed class StockReservationService
    : IStockReservationService
{
    private readonly InventoryDbContext _db;

    public StockReservationService(
        InventoryDbContext db)
    {
        _db = db;
    }

    public async Task<ReservationStatus> ReserveAsync(
        Guid productId,
        int quantity,
        CancellationToken ct = default)
    {
        if (productId == Guid.Empty)
            return ReservationStatus.NotFound;

        if (quantity <= 0)
            return ReservationStatus.invalidQuantity;

        // Verifica e atualiza em um unico comando SQL.
        var affectedRows = await _db.StockItems
            .Where(stock =>
                stock.ProductId == productId &&
                stock.Quantity - stock.ReservedQuantity
                    >= quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    stock => stock.ReservedQuantity,
                    stock => stock.ReservedQuantity + quantity)
                .SetProperty(
                    stock => stock.UpdatedAt,
                    DateTime.UtcNow),
                ct);

        if (affectedRows == 1)
            return ReservationStatus.Reserved;

        // Diferencia produto ausente de saldo insuficiente.
        var exists = await _db.StockItems
            .AsNoTracking()
            .AnyAsync(
                stock => stock.ProductId == productId,
                ct);

        return exists
            ? ReservationStatus.insufficientStock
            : ReservationStatus.NotFound;
    }
}
