using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using backend.Domain.Entities;

namespace ServiceCenterApi.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Name).IsRequired().HasMaxLength(150);
            builder.Property(u => u.PhoneOrEmail).IsRequired().HasMaxLength(150);
            builder.Property(u => u.PasswordHash).IsRequired();

            builder.HasIndex(u => u.PhoneOrEmail).IsUnique();

            // Store enum as readable text rather than an integer.
            builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            builder.HasMany(u => u.Invoices)
                .WithOne(i => i.User)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}