
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orbis.Inventory.Api.Data;
using Orbis.Inventory.Api.Domain;

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
            return ReservationStatus.InsufficientStock;

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
            ? ReservationStatus.InsufficientStock
            : ReservationStatus.NotFound;
    }

    public async Task<ReservationResult> ReserveAsync(
        Guid orderId,
        Guid productId,
        int quantity,
        CancellationToken ct = default)
    {
        if (orderId == Guid.Empty ||
        productId == Guid.Empty ||
        quantity <= 0)
        {
            return new(ReservationStatus.InvalidQuantity);
        }

        // Primeiro, verifica se esta operacao ja foi realizada.
        var existing = await _db.StockReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.OrderId == orderId &&
                x.ProductId == productId, ct);

        if (existing is not null)
            return ResolveExisting(existing, quantity);

        try
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(ct);

            // Atualizacao atomica do saldo.
            var updated = await _db.StockItems
                .Where(x =>
                    x.ProductId == productId &&
                    x.Quantity - x.ReservedQuantity
                        >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        x => x.ReservedQuantity,
                        x => x.ReservedQuantity + quantity)
                    .SetProperty(
                        x => x.UpdatedAt,
                        DateTime.UtcNow),
                    ct);

            if (updated == 0)
            {
                await transaction.RollbackAsync(ct);

                var stockExists = await _db.StockItems
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.ProductId == productId, ct);

                return new(stockExists
                    ? ReservationStatus.InsufficientStock
                    : ReservationStatus.NotFound);
            }

            var reservation = new StockReservation(
                orderId,
                productId,
                quantity);

            _db.StockReservations.Add(reservation);

            await _db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return new(
                ReservationStatus.Reserved,
                reservation.Id);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException pg &&
            pg.SqlState ==
                PostgresErrorCodes.UniqueViolation &&
            pg.ConstraintName ==
                "IX_stock_reservations_OrderId_ProductId")
        {
            // A transacao foi descartada e revertida.
            _db.ChangeTracker.Clear();

            var saved = await _db.StockReservations
                .AsNoTracking()
                .SingleAsync(x =>
                    x.OrderId == orderId &&
                    x.ProductId == productId, ct);

            return ResolveExisting(saved, quantity);
        }
    }


    public async Task<ReleaseStatus> ReleaseAsync(
        Guid orderId,
        Guid productId,
        CancellationToken ct = default)
    {
        if (orderId == Guid.Empty ||
            productId == Guid.Empty)
        {
            return ReleaseStatus.InvalidRequest;
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var reservation = await _db.StockReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.OrderId == orderId &&
                x.ProductId == productId, ct);

        if (reservation is null)
            return ReleaseStatus.NotFound;

        if (reservation.State ==
            StockReservationState.Released)
        {
            return ReleaseStatus.AlreadyReleased;
        }

        // Somente uma execucao pode mudar Reserved -> Released.
        var changed = await _db.StockReservations
            .Where(x =>
                x.Id == reservation.Id &&
                x.State == StockReservationState.Reserved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    x => x.State,
                    StockReservationState.Released)
                .SetProperty(
                    x => x.ReleasedAt,
                    DateTime.UtcNow),
                ct);

        if (changed == 0)
        {
            // Outra requisicao ja pode ter liberado a reserva.
            return ReleaseStatus.AlreadyReleased;
        }

        var updatedStock = await _db.StockItems
            .Where(x =>
                x.ProductId == productId &&
                x.ReservedQuantity >= reservation.Quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    x => x.ReservedQuantity,
                    x => x.ReservedQuantity - reservation.Quantity)
                .SetProperty(
                    x => x.UpdatedAt,
                    DateTime.UtcNow),
                ct);

        if (updatedStock != 1)
        {
            throw new InvalidOperationException(
                "Inconsistencia no saldo reservado.");
        }

        await transaction.CommitAsync(ct);

        return ReleaseStatus.Released;
    }

    private static ReservationResult ResolveExisting(
    StockReservation reservation,
    int requestedQuantity)
    {
        if (reservation.Quantity != requestedQuantity ||
            reservation.State != StockReservationState.Reserved)
        {
            return new(
                ReservationStatus.Conflict,
                reservation.Id);
        }

        return new(
            ReservationStatus.AlreadyReserved,
            reservation.Id);
    }

}
