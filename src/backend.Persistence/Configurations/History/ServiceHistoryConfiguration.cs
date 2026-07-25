using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities.History;

namespace ServiceCenterApi.Data.Configurations.History
{
    public class ServiceHistoryConfiguration : IEntityTypeConfiguration<ServiceHistory>
    {
        public void Configure(EntityTypeBuilder<ServiceHistory> builder)
        {
            builder.ToTable("ServiceHistory", schema: "history");
            builder.HasKey(h => h.Id);

            builder.Property(h => h.CustomerNameSnapshot).IsRequired().HasMaxLength(150);
            builder.Property(h => h.CustomerPhoneSnapshot).IsRequired().HasMaxLength(30);
            builder.Property(h => h.VehiclePlateSnapshot).IsRequired().HasMaxLength(20);
            builder.Property(h => h.VehicleMakeModelSnapshot).HasMaxLength(120);
            builder.Property(h => h.ServicesSummary).HasMaxLength(500);
            builder.Property(h => h.TotalAmount).HasColumnType("decimal(10,2)");
            builder.Property(h => h.MechanicNotes).HasMaxLength(1000);
            builder.Property(h => h.VehicleConditionNotes).HasMaxLength(1000);

            // Deliberately NOT a foreign key - InvoiceId/CustomerId/VehicleId are
            // plain columns for traceability, kept independent of the
            // service_center schema so this stays a standalone, fast read layer.
            builder.HasIndex(h => h.InvoiceId);
            builder.HasIndex(h => h.CustomerId);
            builder.HasIndex(h => h.VehicleId);
            builder.HasIndex(h => h.ServiceDate);
        }
    }
}