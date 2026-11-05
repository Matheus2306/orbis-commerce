using Orbis.Catalog.Api.Contracts;

namespace Orbis.Catalog.Api.Application;

public enum CreateProductStatus
{
    Created,
    Invalid,
    DuplicateSku
}

public sealed record CreateProductResult(
    CreateProductStatus Status,
    ProductResponse? Product = null,
    string? Error = null
);