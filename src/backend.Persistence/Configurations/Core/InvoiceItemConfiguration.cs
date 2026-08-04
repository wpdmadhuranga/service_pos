using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities;

namespace ServiceCenterApi.Data.Configurations
{
    public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            builder.ToTable("InvoiceItems");
            builder.HasKey(ii => ii.Id);

            builder.Property(ii => ii.NameSnapshot).IsRequired().HasMaxLength(150);
            builder.Property(ii => ii.PriceSnapshot).HasColumnType("decimal(10,2)");
            builder.Property(ii => ii.LineTotal).HasColumnType("decimal(10,2)");
            builder.Property(ii => ii.Quantity).HasDefaultValue(1);
            builder.Property(ii => ii.BrandSnapshot).HasMaxLength(100);

            builder.HasIndex(ii => ii.InvoiceId);
            builder.HasIndex(ii => ii.ProductId);

            builder.HasOne(ii => ii.Invoice)
                .WithMany(i => i.InvoiceItems)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ServiceId is nullable - a custom one-off line item has no catalog service.
            builder.HasOne(ii => ii.Service)
                .WithMany(s => s.InvoiceItems)
                .HasForeignKey(ii => ii.ServiceId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(ii => ii.Product)
                .WithMany(p => p.InvoiceItems)
                .HasForeignKey(ii => ii.ProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}