using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities.Inventory;

namespace backend.Persistence.Configurations.Inventory
{
    public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
    {
        public void Configure(EntityTypeBuilder<InventoryItem> builder)
        {
            builder.ToTable("InventoryItems", schema: "inventory");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Name).IsRequired().HasMaxLength(150);
            builder.Property(i => i.Sku).HasMaxLength(60);
            builder.Property(i => i.Unit).IsRequired().HasMaxLength(20);
            builder.Property(i => i.QuantityOnHand).HasColumnType("decimal(10,2)");
            builder.Property(i => i.ReorderLevel).HasColumnType("decimal(10,2)");
            builder.Property(i => i.UnitCost).HasColumnType("decimal(10,2)");

            builder.HasIndex(i => i.Sku)
                .IsUnique()
                .HasFilter("\"Sku\" IS NOT NULL");

            builder.HasIndex(i => i.IsActive);
        }
    }
}