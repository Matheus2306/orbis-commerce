using MassTransit;
using Moq;

using Orbis.Catalog.Api.Application;
using Orbis.Catalog.Api.Contracts;
using Orbis.Catalog.Api.Domain;
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

        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());

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


    [Fact]
    public async Task CreateAsync_NegativePrice_ShouldReject()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());

        var request = new CreateProductRequest(
            "Monitor",
            "ORB-MON-001",
            -150m
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.Equal(
            CreateProductStatus.Invalid,
            result.Status
        );

        Assert.Null(result.Product);

        Assert.Equal(0, repository.SaveCalls);

        Assert.Equal(0, await repository.CountAsync());
    }


    [Fact]
    public async Task CreateAsync_DuplicateSku_ShouldReturnConflict()
    {
        // Arrange
        var repository = new FakeProductRepository();

        await repository.AddAsync(
            new Product(
                "Notebook Original",
                "ORB-001",
                3000m
            )
        );

        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());

        var request = new CreateProductRequest(
            "Outro Notebook",
            "ORB-001",
            4500m
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.Equal(
            CreateProductStatus.DuplicateSku,
            result.Status
        );

        Assert.Equal(0, repository.SaveCalls);

        Assert.Equal(1, await repository.CountAsync());
    }

    [Fact]
    public async Task SearchAsync_Exists_Product_by_Id()
    {
        var repository = new FakeProductRepository();

        var product = new Product(
            "Notebook Original",
            "ORB-001",
            3000m
        );

        await repository.AddAsync(
            product
        );

        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());


        var exists = await service.ExistsByIdAsync(product.Id);

        Assert.True(exists);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task SearchAsync_Inexistent_Product_by_Id()
    {
        var repository = new FakeProductRepository();

        var product = new Product(
            "Notebook Original",
            "ORB-001",
            3000m
        );

        await repository.AddAsync(
            product
        );

        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());

        var newGuid = Guid.NewGuid();

        var exists = await service.ExistsByIdAsync(newGuid);

        Assert.False(exists);
        Assert.Equal(0, repository.SaveCalls);
    }
    [Fact]
    public async Task CountAsync()
    {
        var repository = new FakeProductRepository();

        await repository.AddAsync(
            new Product(
                "Notebook Original",
                "ORB-001",
                3000m
            )
        );
        await repository.AddAsync(
            new Product(
                "Notebook Original",
                "ORB-002",
                3000m
            )
        );
        await repository.AddAsync(
            new Product(
                "Notebook Original",
                "ORB-003",
                3000m
            )
        );

        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());

        var totalCount = await service.CountAsync();

        Assert.Equal(3, totalCount);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task GetByIdAsync()
    {
        var repository = new FakeProductRepository();

        var product = new Product(
            name: "Teclado",
            sku: "ORB-001",
            2213m
        );

        await repository.AddAsync(product);

        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());

        var result = await service.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal("Teclado", result.Name);
        Assert.Equal("ORB-001", result.Sku);
        Assert.Equal(2213m, result.Price);
    }

    [Fact]
    public async Task GetByIdAsync_Inexistente()
    {
        var repository = new FakeProductRepository();

        var product = new Product(
            name: "Teclado Mecânico",
            sku: "ORB-002",
            3213m
        );

        await repository.AddAsync(product);

        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());

        var id = Guid.NewGuid();

        var result = await service.GetByIdAsync(id);

        Assert.Null(result);
    }
}
