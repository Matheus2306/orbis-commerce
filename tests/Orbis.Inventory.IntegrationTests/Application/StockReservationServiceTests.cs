using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Orbis.Inventory.Api.Application;
using Orbis.Inventory.Api.Data;
using Orbis.Inventory.Api.Domain;

namespace Orbis.Inventory.IntegrationTests.Application;

public sealed class StockReservationServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private InventoryDbContext _db = null!;
    private StockReservationService _service = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _service = new StockReservationService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<StockItem> Seed(int quantity = 10, int reserved = 2)
    {
        var stock = new StockItem(Guid.NewGuid(), quantity, reserved);
        _db.StockItems.Add(stock);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return stock;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public async Task Reserve_AvailableStock_PersistsReservation(int quantity)
    {
        var stock = await Seed();
        var other = await Seed();
        var before = DateTime.UtcNow;

        Assert.Equal(ReservationStatus.Reserved, await _service.ReserveAsync(stock.ProductId, quantity));

        var saved = await _db.StockItems.AsNoTracking().SingleAsync(x => x.Id == stock.Id);
        Assert.Equal(10, saved.Quantity);
        Assert.Equal(2 + quantity, saved.ReservedQuantity);
        Assert.Equal(8 - quantity, saved.AvaliableQuantity);
        Assert.InRange(saved.UpdatedAt, before, DateTime.UtcNow);
        var untouched = await _db.StockItems.AsNoTracking().SingleAsync(x => x.Id == other.Id);
        Assert.Equal(other.ReservedQuantity, untouched.ReservedQuantity);
        Assert.Equal(other.UpdatedAt, untouched.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task Reserve_InvalidQuantity_DoesNotChangeStock(int quantity)
    {
        var stock = await Seed();
        Assert.Equal(ReservationStatus.invalidQuantity, await _service.ReserveAsync(stock.ProductId, quantity));
        await AssertUnchanged(stock);
    }

    [Theory]
    [InlineData(10, 2, 9)]
    [InlineData(10, 10, 1)]
    [InlineData(0, 0, 1)]
    [InlineData(10, 2, int.MaxValue)]
    public async Task Reserve_InsufficientAvailability_DoesNotChangeStock(int total, int reserved, int requested)
    {
        var stock = await Seed(total, reserved);
        Assert.Equal(ReservationStatus.insufficientStock, await _service.ReserveAsync(stock.ProductId, requested));
        await AssertUnchanged(stock);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reserve_MissingProduct_ReturnsNotFound(bool emptyId)
    {
        var stock = await Seed();
        Assert.Equal(ReservationStatus.NotFound,
            await _service.ReserveAsync(emptyId ? Guid.Empty : Guid.NewGuid(), 1));
        await AssertUnchanged(stock);
    }

    [Fact]
    public async Task Reserve_RepeatedRequests_UsesRemainingAvailability()
    {
        var stock = await Seed();
        Assert.Equal(ReservationStatus.Reserved, await _service.ReserveAsync(stock.ProductId, 5));
        Assert.Equal(ReservationStatus.insufficientStock, await _service.ReserveAsync(stock.ProductId, 4));
        Assert.Equal(ReservationStatus.Reserved, await _service.ReserveAsync(stock.ProductId, 3));
        Assert.Equal(ReservationStatus.insufficientStock, await _service.ReserveAsync(stock.ProductId, 1));
        var saved = await _db.StockItems.AsNoTracking().SingleAsync();
        Assert.Equal(10, saved.ReservedQuantity);
        Assert.Equal(0, saved.AvaliableQuantity);
    }

    [Fact]
    public async Task Reserve_CanceledRequest_DoesNotChangeStock()
    {
        var stock = await Seed();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _service.ReserveAsync(stock.ProductId, 1, cts.Token));
        await AssertUnchanged(stock);
    }

    private async Task AssertUnchanged(StockItem original)
    {
        var saved = await _db.StockItems.AsNoTracking().SingleAsync(x => x.Id == original.Id);
        Assert.Equal(original.Quantity, saved.Quantity);
        Assert.Equal(original.ReservedQuantity, saved.ReservedQuantity);
        Assert.Equal(original.UpdatedAt, saved.UpdatedAt);
    }
}
