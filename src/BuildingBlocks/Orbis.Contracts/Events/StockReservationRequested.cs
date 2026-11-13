namespace Orbis.Contracts.Events;

public sealed record StockReservationRequested(
    Guid EventId,
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    DateTime OccurredAt
);