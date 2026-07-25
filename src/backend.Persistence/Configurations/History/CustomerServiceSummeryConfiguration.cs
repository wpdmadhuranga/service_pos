using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities.History;

namespace ServiceCenterApi.Data.Configurations.History
{
    public class CustomerServiceSummaryConfiguration : IEntityTypeConfiguration<CustomerServiceSummary>
    {
        public void Configure(EntityTypeBuilder<CustomerServiceSummary> builder)
        {
            builder.ToTable("CustomerServiceSummary", schema: "history");
            builder.HasKey(s => s.CustomerId); // CustomerId IS the primary key - one row per customer

            builder.Property(s => s.TotalSpent).HasColumnType("decimal(10,2)");
            builder.Property(s => s.AverageSpendPerVisit).HasColumnType("decimal(10,2)");

            // Powers the "best customers" report directly.
            builder.HasIndex(s => s.TotalSpent);
            builder.HasIndex(s => s.TotalVisits);
        }
    }
}