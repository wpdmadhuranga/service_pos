using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities.Inventory;

namespace ServiceCenterApi.Data.Configurations.Inventory
{
    public class InvoiceItemInventoryUsageConfiguration : IEntityTypeConfiguration<InvoiceItemInventoryUsage>
    {
        public void Configure(EntityTypeBuilder<InvoiceItemInventoryUsage> builder)
        {
            builder.ToTable("InvoiceItemInventoryUsages", schema: "inventory");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.QuantityUsed).HasColumnType("decimal(10,2)");

            builder.HasIndex(u => u.InvoiceItemId);

            // Cross-schema link to service_center.InvoiceItems - no inverse
            // navigation added there, so InvoiceItem.cs stays untouched.
            builder.HasOne(u => u.InvoiceItem)
                .WithMany()
                .HasForeignKey(u => u.InvoiceItemId)
                .OnDelete(DeleteBehavior.Cascade); // usage record is meaningless without its invoice item

            builder.HasOne(u => u.InventoryItem)
                .WithMany(i => i.UsageRecords)
                .HasForeignKey(u => u.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}