namespace Orbis.Catalog.Api.Domain;

public sealed class Product
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Product() {}

    public Product(string name, string sku, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException(
                "O SKU é obrigatório.");

        if (price < 0)
            throw new ArgumentException(
                "O preço não pode ser negativo.");

        Name = name.Trim();
        Sku = sku.Trim();
        Price = price;
    }
}