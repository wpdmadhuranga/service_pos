using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities.Inventory;
using backend.Domain.Entities;

namespace backend.Persistence.Configurations.Inventory
{
    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions", schema: "inventory");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.Quantity).HasColumnType("decimal(10,2)");
            builder.Property(t => t.Note).HasMaxLength(500);

            builder.HasIndex(t => t.InventoryItemId);
            builder.HasIndex(t => t.CreatedAt);

            // Ledger entries should never disappear - restrict delete of the parent item.
            builder.HasOne(t => t.InventoryItem)
                .WithMany(i => i.Transactions)
                .HasForeignKey(t => t.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cross-schema link to service_center.InvoiceItems - no inverse
            // navigation added there, so InvoiceItem.cs stays untouched.
            builder.HasOne<InvoiceItem>()
                .WithMany()
                .HasForeignKey(t => t.ReferenceInvoiceItemId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Cross-schema link to service_center.Users - who recorded this transaction.
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}