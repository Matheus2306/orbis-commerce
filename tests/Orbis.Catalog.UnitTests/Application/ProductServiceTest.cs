
using Orbis.Catalog.Api.Application;
using Orbis.Catalog.Api.Contracts;
using Orbis.Catalog.UnitTests.Fakes;
using Xunit;

namespace Orbis.Catalog.UnitTests.Application;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidProduct_ShouldCreate()
    {
        // Arrange
        var repository = new FakeProductRepository();

        var service = new ProductService(repository);

        var request = new CreateProductRequest(
            "Notebook Gamer",
            "ORB-NOTE-001",
            4599.90m
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.Equal(
            CreateProductStatus.Created,
            result.Status
        );

        Assert.NotNull(result.Product);

        Assert.Equal(
            "ORB-NOTE-001",
            result.Product.Sku
        );

        Assert.Equal(1, repository.SaveCalls);

        Assert.Equal(
            1,
            await repository.CountAsync()
        );
    }
}
//etapa 4