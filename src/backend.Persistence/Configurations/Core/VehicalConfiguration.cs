using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities;

namespace ServiceCenterApi.Data.Configurations
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.ToTable("Vehicles");
            builder.HasKey(v => v.Id);

            builder.Property(v => v.PlateNumber).IsRequired().HasMaxLength(20);
            builder.Property(v => v.Make).HasMaxLength(60);
            builder.Property(v => v.Model).HasMaxLength(60);
            builder.Property(v => v.VehicleType).HasMaxLength(30);

            // Only enforce uniqueness among non-deleted vehicles, in case a
            // plate number is re-registered on a new vehicle over time.
            builder.HasIndex(v => v.PlateNumber)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            builder.HasQueryFilter(v => v.DeletedAt == null);

            builder.HasOne(v => v.Customer)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(v => v.Invoices)
                .WithOne(i => i.Vehicle)
                .HasForeignKey(i => i.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}