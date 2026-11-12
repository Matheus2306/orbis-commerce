using MassTransit;
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
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

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

            entity.Property(x => x.ReservedQuantity).HasDefaultValue(0).IsRequired();

            entity.Ignore(x => x.AvaliableQuantity);

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_stock_items_quantities",
                "\"Quantity\" >= 0 AND " +
                "\"ReservedQuantity\" >= 0 AND " +
                "\"ReservedQuantity\" <= \"Quantity\""));
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();


        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("stock_reservations");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.OrderId)
                .IsRequired();

            entity.Property(x => x.ProductId)
                .IsRequired();

            entity.Property(x => x.Quantity)
                .IsRequired();

            entity.Property(x => x.State)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.OrderId,
                x.ProductId
            }).IsUnique();

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_stock_reservations_quantity",
                    "\"Quantity\" > 0"));
        });

    }
}
