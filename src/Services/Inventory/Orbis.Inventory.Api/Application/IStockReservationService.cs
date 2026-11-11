using System;

namespace Orbis.Inventory.Api.Application;

public enum ReservationStatus
{
    Reserved,
    AlreadyReserved,
    InsufficientStock,
    NotFound,
    InvalidQuantity,
    Conflict
}

public enum ReleaseStatus
{
    Released,
    AlreadyReleased,
    NotFound,
    InvalidRequest
}

public sealed record ReservationResult(
    ReservationStatus Status,
    Guid? ReservationId = null);

public interface IStockReservationService
{
    Task<ReservationStatus> ReserveAsync(
        Guid productId,
        int quantity,
        CancellationToken ct = default);

    Task<ReservationResult> ReserveAsync(
        Guid orderId,
        Guid productId,
        int quantity,
        CancellationToken ct = default);

    Task<ReleaseStatus> ReleaseAsync(
        Guid orderId,
        Guid productId,
        CancellationToken ct = default);
}
