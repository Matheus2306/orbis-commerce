namespace Orbis.Catalog.Api.Application;

public sealed class DuplicateProductSkuException : Exception
{
    public DuplicateProductSkuException(
        string sku,
        Exception? innerException = null)
        : base($"O SKU '{sku}' já está cadastrado.",
               innerException)
    {
    }
}
