
using Orbis.Catalog.Api.Domain;

namespace Orbis.Catalog.Api.Contracts;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    DateTime CreatedAt
)
{
    public static ProductResponse FromEntity(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Sku,
            product.Price,
            product.CreatedAt
        );
    }
}
