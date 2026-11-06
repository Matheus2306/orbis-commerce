
using Microsoft.EntityFrameworkCore;
using Orbis.Catalog.Api.Data;
using Orbis.Catalog.Api.Infrastructure.Repositories;
using Orbis.Catalog.Api.Domain;
using Testcontainers.PostgreSql;
using Xunit;
using Orbis.Catalog.Api.Application;

namespace Orbis.Catalog.IntegrationTests.Infrastructure;

public sealed class ProductRepositoryTests
{
    [Fact]
    public async Task Migrations_ShouldCreateProductsTable()
    {
        // Arrange: cria um PostgreSQL temporario
        await using var postgres = new PostgreSqlBuilder(
            "postgres:17-alpine"
        ).Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        await using var db = new CatalogDbContext(options);

        // Act: aplica as migrations existentes
        await db.Database.MigrateAsync();

        // Assert: consulta a tabela no banco real
        var canQuery = await db.Products
            .AsNoTracking()
            .AnyAsync();

        Assert.False(canQuery);
    }

    [Fact]
    public async Task AddAsync_ValidProduct_ShouldPersist()
    {
        // Arrange
        await using var postgres = new PostgreSqlBuilder(
            "postgres:17-alpine"
        ).Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        // Prepara o esquema
        await using (var setup = new CatalogDbContext(options))
        {
            await setup.Database.MigrateAsync();
        }

        var product = new Product(
            "Notebook Gamer",
            "ORB-NOTE-001",
            4599.90m
        );

        // Act: grava o produto
        await using (var writeDb = new CatalogDbContext(options))
        {
            var repository = new ProductRepository(writeDb);

            await repository.AddAsync(product);
        }

        // Assert: consulta com outro DbContext
        await using (var readDb = new CatalogDbContext(options))
        {
            var repository = new ProductRepository(readDb);

            var result = await repository.GetByIdAsync(product.Id);

            Assert.NotNull(result);
            Assert.Equal("Notebook Gamer", result.Name);
            Assert.Equal("ORB-NOTE-001", result.Sku);
            Assert.Equal(4599.90m, result.Price);
        }
    }


    [Fact]
    public async Task AddAsync_DuplicateSku_ShouldThrow()
    {
        // Arrange
        await using var postgres = new PostgreSqlBuilder(
            "postgres:17-alpine"
        ).Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        await using var db = new CatalogDbContext(options);

        await db.Database.MigrateAsync();

        var repository = new ProductRepository(db);

        var firstProduct = new Product(
            "Notebook A",
            "ORB-001",
            3000m
        );

        var secondProduct = new Product(
            "Notebook B",
            "ORB-001",
            4500m
        );

        await repository.AddAsync(firstProduct);

        // Act + Assert
        await Assert.ThrowsAsync<DuplicateProductSkuException>(
            () => repository.AddAsync(secondProduct)
        );

        var total = await repository.CountAsync();

        Assert.Equal(1, total);
    }
}
