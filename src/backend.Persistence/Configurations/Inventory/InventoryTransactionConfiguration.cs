using backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Persistence.Configurations.Inventory
{
    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions", schema: "inventory");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.Quantity);
            builder.Property(t => t.Notes).HasMaxLength(500);

            builder.HasIndex(t => t.ProductId);
            builder.HasIndex(t => t.InvoiceId);
            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.CreatedAt);

            builder.HasOne(t => t.Product)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Invoice)
                .WithMany()
                .HasForeignKey(t => t.InvoiceId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}