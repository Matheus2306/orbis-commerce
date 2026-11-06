namespace Orbis.Inventory.Api.Contracts;

public sealed record CreateStockRequest(
    Guid ProductId,
    int Quantity
);

public sealed record AddStockRequest(int Quantity);