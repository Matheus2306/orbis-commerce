using Microsoft.EntityFrameworkCore;
using Orbis.Catalog.Api.Data;
using Orbis.Catalog.Api.Infrastructure.Repositories;
using Orbis.Catalog.Api.Domain;
using Testcontainers.PostgreSql;
using Xunit;
using Orbis.Catalog.Api.Application;

namespace Orbis.Catalog.IntegrationTests.Infrastructure;

[Trait("Category", "Docker")]
public sealed class ProductRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private DbContextOptions<CatalogDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        // Aplica as migrações no banco ao iniciar a classe de testes
        await using var context = new CatalogDbContext(_options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Migrations_ShouldCreateProductsTable()
    {
        await using var context = new CatalogDbContext(_options);

        // Consulta a tabela no banco real
        var canQuery = await context.Products
            .AsNoTracking()
            .AnyAsync();

        Assert.False(canQuery);
    }

    [Fact]
    public async Task AddAsync_ValidProduct_ShouldPersist()
    {
        var product = new Product(
            "Notebook Gamer",
            "ORB-NOTE-001",
            4599.90m
        );

        // Act: grava o produto usando um contexto dedicado
        await using (var writeDb = new CatalogDbContext(_options))
        {
            var repository = new ProductRepository(writeDb);
            await repository.AddAsync(product);
            await repository.SaveChangesAsync();
        }

        // Assert: consulta com outro DbContext
        await using (var readDb = new CatalogDbContext(_options))
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
        await using var db = new CatalogDbContext(_options);
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
        await repository.SaveChangesAsync();
        await repository.AddAsync(secondProduct);

        // Act + Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.SaveChangesAsync()
        );

        var total = await repository.CountAsync();
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task ExistsBySkuAsync_ShouldReturnTrue()
    {
        await using var db = new CatalogDbContext(_options);

        var product = new Product(
            "Notebook Pro",
            "ORB-SKU-999",
            7000m
        );

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var repository = new ProductRepository(db);
        var exists = await repository.ExistsBySkuAsync("ORB-SKU-999");

        Assert.True(exists);
    }

    [Fact]
    public async Task CountAsync_ThreeProducts_ShouldReturnThree()
    {
        await using var db = new CatalogDbContext(_options);

        db.Products.Add(new Product(
            "Notebook Pro",
            "ORB-SKU-999",
            7000m
        ));
        db.Products.Add(new Product(
            "Notebook Pro 2",
            "ORB-SKU-998",
            7000m
        ));
        db.Products.Add(new Product(
            "Notebook Pro 3",
            "ORB-SKU-997",
            7000m
        ));

        db.SaveChanges();

        var repository = new ProductRepository(db);

        // Act
        var result = await repository.CountAsync();

        // Assert
        Assert.Equal(3, result);
    }
}
