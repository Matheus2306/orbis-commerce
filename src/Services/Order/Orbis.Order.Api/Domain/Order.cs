
namespace Orbis.Order.Api.Domain;

public sealed class Order
{
    public Guid Id { get; private set; }
        = Guid.NewGuid();

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private Order() { }

    public Order(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException(
                "Produto inválido.");

        if (quantity <= 0)
            throw new ArgumentException(
                "A quantidade deve ser positiva.");

        ProductId = productId;
        Quantity = quantity;
        Status = OrderStatus.PendingReservation;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void ConfirmReservation()
    {
        if (Status != OrderStatus.PendingReservation)
            throw new InvalidOperationException(
                "O pedido não está aguardando reserva.");

        Status = OrderStatus.Reserved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RejectReservation()
    {
        if (Status != OrderStatus.PendingReservation)
            throw new InvalidOperationException(
                "O pedido não está aguardando reserva.");

        Status = OrderStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }
}
