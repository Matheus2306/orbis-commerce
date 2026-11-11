using Microsoft.EntityFrameworkCore;
using Orbis.Inventory.Api.Application;
using Orbis.Inventory.Api.Data;
using Orbis.Inventory.Api.Domain;
using Testcontainers.PostgreSql;

namespace Orbis.Inventory.IntegrationTests.Application;

public sealed class StockReservationConcurrencyTests
{
    [Fact]
    [Trait("Category", "Docker")]
    public async Task Reserve_ConcurrentRequests_NeverOversells()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(postgres.GetConnectionString()).Options;
        var productId = Guid.NewGuid();
        await using (var seed = new InventoryDbContext(options))
        {
            await seed.Database.MigrateAsync();
            seed.StockItems.Add(new StockItem(productId, 10, 0));
            await seed.SaveChangesAsync();
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var db = new InventoryDbContext(options);
            await start.Task;
            return await new StockReservationService(db).ReserveAsync(productId, 1);
        }).ToArray();
        start.SetResult();
        var results = await Task.WhenAll(requests);

        Assert.Equal(10, results.Count(x => x == ReservationStatus.Reserved));
        Assert.Equal(10, results.Count(x => x == ReservationStatus.insufficientStock));
        await using var read = new InventoryDbContext(options);
        var stock = await read.StockItems.AsNoTracking().SingleAsync();
        Assert.Equal(10, stock.Quantity);
        Assert.Equal(10, stock.ReservedQuantity);
        Assert.Equal(0, stock.AvaliableQuantity);
    }
}
