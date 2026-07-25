using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities;

namespace ServiceCenterApi.Data.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(i => i.InvoiceNumber).IsUnique();

            builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

            builder.Property(i => i.Subtotal).HasColumnType("decimal(10,2)");
            builder.Property(i => i.Discount).HasColumnType("decimal(10,2)");
            builder.Property(i => i.Tax).HasColumnType("decimal(10,2)");
            builder.Property(i => i.Total).HasColumnType("decimal(10,2)");

            builder.Property(i => i.Notes).HasMaxLength(1000);

            builder.HasIndex(i => i.CreatedAt); // date-range reports
            builder.HasIndex(i => i.CustomerId);
            builder.HasIndex(i => i.VehicleId);

            builder.HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.Vehicle)
                .WithMany(v => v.Invoices)
                .HasForeignKey(i => i.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.User)
                .WithMany(u => u.Invoices)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // An invoice's line items are owned by that invoice - deleting
            // the invoice deletes its items too.
            builder.HasMany(i => i.InvoiceItems)
                .WithOne(ii => ii.Invoice)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(i => i.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}