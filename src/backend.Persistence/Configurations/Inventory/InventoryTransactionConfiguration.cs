using backend.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Persistence.Configurations.Inventory
{
    public class InventoryItemTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryItemTransactions", schema: "inventory");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.Quantity).HasColumnType("decimal(18,4)");
            builder.Property(t => t.Note).HasMaxLength(500);

            builder.HasIndex(t => t.InventoryItemId);
            builder.HasIndex(t => t.CreatedBy);
            builder.HasIndex(t => t.CreatedAt);
            builder.HasIndex(t => t.ReferenceInvoiceItemId);

            builder.HasOne(t => t.InventoryItem)
                .WithMany(i => i.Transactions)
                .HasForeignKey(t => t.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}