using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities.Inventory;

namespace backend.Persistence.Configurations.Inventory
{
    public class ServiceInventoryMappingConfiguration : IEntityTypeConfiguration<ServiceInventoryMapping>
    {
        public void Configure(EntityTypeBuilder<ServiceInventoryMapping> builder)
        {
            builder.ToTable("ServiceInventoryMappings", schema: "inventory");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.DefaultQuantity).HasColumnType("decimal(10,2)");

            // One service shouldn't map to the same inventory item twice.
            builder.HasIndex(m => new { m.ServiceId, m.InventoryItemId }).IsUnique();

            // Cross-schema link to service_center.Services - no inverse
            // navigation added there, so Service.cs stays untouched.
            builder.HasOne(m => m.Service)
                .WithMany()
                .HasForeignKey(m => m.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.InventoryItem)
                .WithMany(i => i.ServiceMappings)
                .HasForeignKey(m => m.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}