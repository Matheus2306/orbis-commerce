namespace Orbis.Inventory.Api.Domain;

public sealed class StockItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public int ReservedQuantity { get; private set; }

    public int AvaliableQuantity => Quantity - ReservedQuantity;

    public DateTime UpdatedAt { get; private set; }
        = DateTime.UtcNow;

    private StockItem() { }

    public StockItem(Guid productId, int quantity, int reservedQuantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException(
                "O identificador do produto é obrigatório.");

        if (quantity < 0)
            throw new ArgumentException(
                "A quantidade não pode ser negativa.");

        ProductId = productId;
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "A quantidade deve ser maior que zero.");

        int newQuantity;

        try
        {
            newQuantity = checked(Quantity + quantity);
        }
        catch (OverflowException)
        {
            throw new ArgumentException(
                "A quantidade excede o limite permitido.");
        }

        Quantity = newQuantity;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Reserved(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.");

        if (AvaliableQuantity < quantity)
            throw new InvalidOperationException("Estoque Insuficiente.");

        ReservedQuantity = checked(ReservedQuantity + quantity);

        UpdatedAt = DateTime.UtcNow;
    }
}
