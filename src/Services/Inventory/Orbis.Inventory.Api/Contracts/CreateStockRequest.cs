namespace Orbis.Inventory.Api.Contracts;

public sealed record CreateStockRequest(
    Guid ProductId,
    int Quantity,
    int ReservedQuantity
);

public sealed record AddStockRequest(int Quantity);

public sealed record ReserveStockRequest(
    Guid OrderId,
    int Quantity
);
