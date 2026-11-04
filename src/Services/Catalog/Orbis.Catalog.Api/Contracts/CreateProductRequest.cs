namespace Orbis.Catalog.Api.Contracts;

public sealed record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price
);