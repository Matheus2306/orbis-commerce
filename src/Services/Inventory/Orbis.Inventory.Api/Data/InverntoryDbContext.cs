
using Microsoft.EntityFrameworkCore;
using Orbis.Inventory.Api.Domain;

namespace Orbis.Inventory.Api.Data;

public sealed class InventoryDbContext : DbContext
{
    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<StockItem> StockItems => Set<StockItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("stock_items");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ProductId)
                .IsRequired();

            entity.HasIndex(x => x.ProductId)
                .IsUnique();

            entity.Property(x => x.Quantity)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();
        });
    }
}
