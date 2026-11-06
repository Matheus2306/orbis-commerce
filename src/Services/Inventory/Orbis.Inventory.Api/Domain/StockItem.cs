namespace Orbis.Inventory.Api.Domain;

public sealed class StockItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public DateTime UpdatedAt { get; private set; }
        = DateTime.UtcNow;

    private StockItem() { }

    public StockItem(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException(
                "O identificador do produto é obrigatório.");

        if (quantity < 0)
            throw new ArgumentException(
                "A quantidade não pode ser negativa.");

        ProductId = productId;
        Quantity = quantity;
    }
}
