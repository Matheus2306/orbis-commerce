using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using Orbis.Catalog.Api.Application;
using Orbis.Catalog.Api.Contracts;
using Orbis.Catalog.Api.Domain;
using Orbis.Catalog.UnitTests.Fakes;
using Orbis.Contracts.Events;

namespace Orbis.Catalog.UnitTests.Application;

public sealed class ProductServiceBehaviorTests
{
    [Theory]
    [InlineData(null, "SKU", 1)]
    [InlineData("", "SKU", 1)]
    [InlineData("  ", "SKU", 1)]
    [InlineData("Product", null, 1)]
    [InlineData("Product", "", 1)]
    [InlineData("Product", "  ", 1)]
    [InlineData("Product", "SKU", -1)]
    public async Task Create_InvalidInput_HasNoSideEffects(string? name, string? sku, int price)
    {
        var repository = new Mock<IProductRepository>(MockBehavior.Strict);
        var publisher = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var result = await new ProductService(repository.Object, publisher.Object)
            .CreateAsync(new CreateProductRequest(name!, sku!, price));

        Assert.Equal(CreateProductStatus.Invalid, result.Status);
        Assert.Null(result.Product);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        repository.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(125)]
    public async Task Create_ValidInput_AddsPublishesThenSaves(int price)
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var sequence = new MockSequence();
        var repository = new Mock<IProductRepository>(MockBehavior.Strict);
        var publisher = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        Product? added = null;
        ProductCreated? published = null;
        repository.InSequence(sequence).Setup(x => x.ExistsBySkuAsync("SKU", token)).ReturnsAsync(false);
        repository.InSequence(sequence).Setup(x => x.AddAsync(It.IsAny<Product>(), token))
            .Callback<Product, CancellationToken>((p, _) => added = p).Returns(Task.CompletedTask);
        publisher.InSequence(sequence).Setup(x => x.Publish(It.IsAny<ProductCreated>(), token))
            .Callback<ProductCreated, CancellationToken>((e, _) => published = e).Returns(Task.CompletedTask);
        repository.InSequence(sequence).Setup(x => x.SaveChangesAsync(token)).ReturnsAsync(1);
        var before = DateTime.UtcNow;

        var result = await new ProductService(repository.Object, publisher.Object)
            .CreateAsync(new CreateProductRequest(" Product ", " SKU ", price), token);

        Assert.Equal(CreateProductStatus.Created, result.Status);
        Assert.NotNull(added);
        Assert.Equal(ProductResponse.FromEntity(added), result.Product);
        Assert.Equal("Product", added.Name);
        Assert.Equal("SKU", added.Sku);
        Assert.Equal(price, added.Price);
        Assert.NotNull(published);
        Assert.Equal(added.Id, published.ProductId);
        Assert.Equal(added.Sku, published.Sku);
        Assert.NotEqual(Guid.Empty, published.EventId);
        Assert.InRange(published.OccurredAt, before, DateTime.UtcNow);
        repository.VerifyAll();
        publisher.VerifyAll();
    }

    [Fact]
    public async Task Create_ExistingNormalizedSku_DoesNotPublishOrSave()
    {
        var repository = new Mock<IProductRepository>(MockBehavior.Strict);
        repository.Setup(x => x.ExistsBySkuAsync("SKU", default)).ReturnsAsync(true);
        var publisher = new Mock<IPublishEndpoint>(MockBehavior.Strict);

        var result = await new ProductService(repository.Object, publisher.Object)
            .CreateAsync(new CreateProductRequest("Product", " SKU ", 10));

        Assert.Equal(CreateProductStatus.DuplicateSku, result.Status);
        Assert.Null(result.Product);
        repository.Verify(x => x.ExistsBySkuAsync("SKU", default), Times.Once);
        repository.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "IX_products_Sku", true)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "other_constraint", false)]
    [InlineData(PostgresErrorCodes.CheckViolation, "IX_products_Sku", false)]
    public async Task Create_SaveFailure_OnlySkuConflictIsTranslated(string code, string constraint, bool duplicate)
    {
        var error = new DbUpdateException("save failed",
            new PostgresException("failure", "ERROR", "ERROR", code, constraintName: constraint));
        var repository = new Mock<IProductRepository>();
        repository.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(error);
        var service = new ProductService(repository.Object, Mock.Of<IPublishEndpoint>());
        var request = new CreateProductRequest("Product", "SKU", 10);

        if (duplicate)
        {
            var result = await service.CreateAsync(request);
            Assert.Equal(CreateProductStatus.DuplicateSku, result.Status);
            Assert.Null(result.Product);
        }
        else
            Assert.Same(error, await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(request)));
    }

    [Fact]
    public async Task Create_PublishFailure_DoesNotSave()
    {
        var repository = new Mock<IProductRepository>();
        var publisher = new Mock<IPublishEndpoint>();
        publisher.Setup(x => x.Publish(It.IsAny<ProductCreated>(), default))
            .ThrowsAsync(new InvalidOperationException("broker failed"));
        var service = new ProductService(repository.Object, publisher.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateProductRequest("Product", "SKU", 10)));

        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reads_EmptyRepository_ReturnsEmptyResults()
    {
        var service = new ProductService(new FakeProductRepository(), Mock.Of<IPublishEndpoint>());
        Assert.Empty(await service.GetAllAsync());
        Assert.Equal(0, await service.CountAsync());
        Assert.False(await service.ExistsBySkuAsync("missing"));
    }

    [Fact]
    public async Task GetAll_MapsEveryProductField()
    {
        var repository = new FakeProductRepository();
        var products = new[] { new Product("A", "A-1", 10), new Product("B", "B-1", 20) };
        foreach (var product in products)
            await repository.AddAsync(product);
        var service = new ProductService(repository, Mock.Of<IPublishEndpoint>());

        Assert.Equal(products.Select(ProductResponse.FromEntity), await service.GetAllAsync());
        Assert.True(await service.ExistsBySkuAsync("A-1"));
        Assert.False(await service.ExistsBySkuAsync("missing"));
    }
}
