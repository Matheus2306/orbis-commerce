using System;

namespace Orbis.Inventory.Api.Application;

public enum ReservationStatus
{
    Reserved,
    insufficientStock,
    NotFound,
    invalidQuantity
}

public interface IStockReservationService
{
    Task<ReservationStatus> ReserveAsync(
        Guid productId,
        int quantity,
        CancellationToken ct = default);
}
