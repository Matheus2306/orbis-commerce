using System;

namespace Orbis.Inventory.Api.Domain;

public enum StockReservationState
{
    Reserved = 1,
    Released = 2
}

public class StockReservation
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public StockReservationState State { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    private StockReservation() { }

    public StockReservation(
        Guid orderId,
        Guid productId,
        int quantity)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Pedido inválido");

        if (productId == Guid.Empty)
            throw new ArgumentNullException("Produto inválido.");

        if (quantity <= 0)
            throw new ArgumentException("Quantidade inválida.");
        
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        State = StockReservationState.Reserved;
        CreatedAt = DateTime.UtcNow;
    }
}
