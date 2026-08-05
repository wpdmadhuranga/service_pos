using Microsoft.EntityFrameworkCore;
using backend.Application.Common.Interfaces;
using backend.Domain.Entities;
using backend.Domain.Entities.History;
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

        // schema: service_center
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<User> Users => Set<User>();

        // schema: audit
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // schema: history
        public DbSet<ServiceHistory> ServiceHistory => Set<ServiceHistory>();
        public DbSet<CustomerServiceSummary> CustomerServiceSummaries => Set<CustomerServiceSummary>();

        IQueryable<User> IApplicationDbContext.Users => Users;
        IQueryable<Customer> IApplicationDbContext.Customers => Customers;
        IQueryable<Vehicle> IApplicationDbContext.Vehicles => Vehicles;
        IQueryable<ServiceCategory> IApplicationDbContext.ServiceCategories => ServiceCategories;
        IQueryable<Service> IApplicationDbContext.Services => Services;
        IQueryable<Product> IApplicationDbContext.Products => Products;
        IQueryable<Invoice> IApplicationDbContext.Invoices => Invoices;
        IQueryable<InvoiceItem> IApplicationDbContext.InvoiceItems => InvoiceItems;
        IQueryable<Payment> IApplicationDbContext.Payments => Payments;
        IQueryable<InventoryTransaction> IApplicationDbContext.InventoryTransactions => InventoryTransactions;

        void IApplicationDbContext.Add<TEntity>(TEntity entity)
        {
            Set<TEntity>().Add(entity);
        }

        void IApplicationDbContext.Remove<TEntity>(TEntity entity)
        {
            Set<TEntity>().Remove(entity);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("service_center");
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
                else if (entry.Entity is Product p)
                {
                    if (entry.State == EntityState.Added) p.CreatedAt = now;
                    if (entry.State is EntityState.Added or EntityState.Modified) p.UpdatedAt = now;
                }
                else if (entry.Entity is Invoice i)
                {
                    if (entry.State == EntityState.Added) i.CreatedAt = now;
                    if (entry.State is EntityState.Added or EntityState.Modified) i.UpdatedAt = now;
                }
                else if (entry.Entity is InventoryTransaction t)
                {
                    if (entry.State == EntityState.Added) t.CreatedAt = now;
                }
            }
        }
    }
}