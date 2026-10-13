using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.Property(p => p.Sku).IsRequired().HasMaxLength(40);
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(p => p.Name);

        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.UnitPrice).HasPrecision(18, 2);

        builder.Property(p => p.ConcurrencyStamp).IsConcurrencyToken();

        builder.Ignore(p => p.IsLowStock);

        builder.HasOne(p => p.Category)
               .WithMany()
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.Property(m => m.Reason).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.PerformedBy).IsRequired().HasMaxLength(256);
        builder.Property(m => m.Reference).HasMaxLength(100);

        builder.HasOne(m => m.Product)
               .WithMany()
               .HasForeignKey(m => m.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ProductId, m.OccurredAtUtc });
    }
}