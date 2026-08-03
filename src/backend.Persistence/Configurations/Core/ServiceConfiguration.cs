using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities;
using backend.Domain.Enums;

namespace ServiceCenterApi.Data.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("Services");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
            builder.Property(s => s.Description).HasMaxLength(500);
            builder.Property(s => s.Unit).HasMaxLength(20);
            builder.Property(s => s.DefaultPrice).HasColumnType("decimal(10,2)");
            builder.Property(s => s.PricingType).HasConversion<string>().HasMaxLength(20).HasDefaultValue(PricingType.Fixed);
            builder.Property(s => s.MinPrice).HasColumnType("decimal(10,2)");
            builder.Property(s => s.MaxPrice).HasColumnType("decimal(10,2)");

            builder.HasIndex(s => s.IsActive);

            builder.HasOne(s => s.Category)
                .WithMany(sc => sc.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.InvoiceItems)
                .WithOne(ii => ii.Service)
                .HasForeignKey(ii => ii.ServiceId)
                .OnDelete(DeleteBehavior.SetNull); // keep the invoice item if the service is later removed
        }
    }
}