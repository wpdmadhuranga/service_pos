using backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Persistence.Configurations.Inventory
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products", schema: "inventory");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Brand).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
            builder.Property(p => p.PartNumber).HasMaxLength(60);
            builder.Property(p => p.CompatibleVehicleType).HasMaxLength(60);
            builder.Property(p => p.CostPrice).HasColumnType("decimal(10,2)");
            builder.Property(p => p.SellingPrice).HasColumnType("decimal(10,2)");
            builder.Property(p => p.Unit).HasMaxLength(20);

            builder.HasIndex(p => p.ServiceId);
            builder.HasIndex(p => p.IsActive);
            builder.HasIndex(p => new { p.ServiceId, p.Brand, p.Name });

            builder.HasOne(p => p.Service)
                .WithMany()
                .HasForeignKey(p => p.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(p => p.InventoryTransactions)
                .WithOne(t => t.Product)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.InvoiceItems)
                .WithOne(ii => ii.Product)
                .HasForeignKey(ii => ii.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}