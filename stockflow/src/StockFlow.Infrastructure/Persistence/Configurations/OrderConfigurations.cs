using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.Property(o => o.Number).IsRequired().HasMaxLength(40);
        builder.HasIndex(o => o.Number).IsUnique();

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PlacedBy).IsRequired().HasMaxLength(256);
        builder.Property(o => o.ConcurrencyStamp).IsConcurrencyToken();
        builder.Ignore(o => o.Total);
        builder.HasIndex(o => o.PlacedAtUtc);

        builder.HasOne(o => o.Customer)
               .WithMany()
               .HasForeignKey(o => o.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Lines)
               .WithOne()
               .HasForeignKey(l => l.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.Property(l => l.ProductSku).IsRequired().HasMaxLength(40);
        builder.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(l => l.LineTotal);

        builder.HasOne<Product>()
               .WithMany()
               .HasForeignKey(l => l.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}