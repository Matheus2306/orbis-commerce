namespace Orbis.Contracts.Events;

public sealed record ProductCreated(
    Guid EventId,
    Guid ProductId,
    string Sku,
    DateTime OccurredAt
);