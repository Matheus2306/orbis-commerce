using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orbis.Catalog.Api.Domain;

namespace Orbis.Catalog.Api.Data;

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
        
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(p => p.Sku)
                .HasMaxLength(60)
                .IsRequired();

            entity.HasIndex(p => p.Sku)
                .IsUnique();

            entity.Property(p => p.Price)
                .HasPrecision(18, 2);

            entity.Property(p => p.CreatedAt)
                .IsRequired();
        });

        // Entidades do Transactions Outbox
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
