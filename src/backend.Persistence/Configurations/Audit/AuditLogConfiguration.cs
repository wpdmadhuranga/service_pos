using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities.Audit;
using backend.Domain.Entities;

namespace backend.Persistence.Configurations.Audit
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs", schema: "audit");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.TableName).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(20);
            builder.Property(a => a.IpAddress).HasMaxLength(45); // fits IPv6

            // These can be large - nvarchar(max) / text, no explicit length cap.
            builder.Property(a => a.OldValues);
            builder.Property(a => a.NewValues);

            builder.HasIndex(a => new { a.TableName, a.RecordId }); // "show history for this record"
            builder.HasIndex(a => a.ChangedAt);

            // Cross-schema link to service_center.Users - no inverse
            // navigation added there, so User.cs stays untouched.
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.ChangedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}