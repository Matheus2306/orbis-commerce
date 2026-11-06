namespace Orbis.Inventory.Api.Contracts;

public sealed record CreateStockRequest(
    Guid ProductId,
    int Quantity
);
