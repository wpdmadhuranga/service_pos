using Microsoft.EntityFrameworkCore;
using backend.Application.Common.Interfaces;
using backend.Domain.Entities;
using backend.Domain.Entities.History;
using backend.Domain.Entities.Inventory;
using backend.Domain.Entities.Audit;


namespace backend.Persistence.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
  // schema: service_center (or dbo, if you didn't rename the default)
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<User> Users => Set<User>();
 
        // schema: inventory
        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<ServiceInventoryMapping> ServiceInventoryMappings => Set<ServiceInventoryMapping>();
        public DbSet<InvoiceItemInventoryUsage> InvoiceItemInventoryUsages => Set<InvoiceItemInventoryUsage>();
 
        // schema: audit
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
 
        // schema: history
        public DbSet<ServiceHistory> ServiceHistory => Set<ServiceHistory>();
        public DbSet<CustomerServiceSummary> CustomerServiceSummaries => Set<CustomerServiceSummary>();

        IQueryable<User> IApplicationDbContext.Users => Users;
 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Customers/Vehicles/Services/Invoices/Users live here. inventory,
            // audit, security, and history each set their own schema explicitly
            // in their configuration files, so this only affects the core tables.
            modelBuilder.HasDefaultSchema("service_center");
 
            // Picks up every IEntityTypeConfiguration<T> class in this assembly -
            // add a new configuration file later and it's wired in automatically.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
 
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }
 
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }
 
        private void UpdateTimestamps()
        {
            var now = DateTime.UtcNow;
 
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is Customer c)
                {
                    if (entry.State == EntityState.Added) c.CreatedAt = now;
                    if (entry.State is EntityState.Added or EntityState.Modified) c.UpdatedAt = now;
                }
                else if (entry.Entity is Vehicle v)
                {
                    if (entry.State == EntityState.Added) v.CreatedAt = now;
                    if (entry.State is EntityState.Added or EntityState.Modified) v.UpdatedAt = now;
                }
                else if (entry.Entity is Service s)
                {
                    if (entry.State == EntityState.Added) s.CreatedAt = now;
                    if (entry.State is EntityState.Added or EntityState.Modified) s.UpdatedAt = now;
                }
                else if (entry.Entity is Invoice i)
                {
                    if (entry.State == EntityState.Added) i.CreatedAt = now;
                    if (entry.State is EntityState.Added or EntityState.Modified) i.UpdatedAt = now;
                }
                else if (entry.Entity is InventoryItem inv)
                {
                    if (entry.State == EntityState.Added) inv.CreatedAt = now;
                    if (entry.State is EntityState.Added or EntityState.Modified) inv.UpdatedAt = now;
                }
            }
        }
    }
}