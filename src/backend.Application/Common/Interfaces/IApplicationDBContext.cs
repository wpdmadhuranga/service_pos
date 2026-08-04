using backend.Domain.Entities;

namespace backend.Application.Common.Interfaces
{
    // Implemented by ApplicationDbContext in Persistence.
    // Lets Application/Infrastructure query the database without referencing
    // Persistence directly - keeps the dependency pointing inward.
    public interface IApplicationDbContext
    {
        IQueryable<Customer> Customers { get; }
        IQueryable<Vehicle> Vehicles { get; }
        IQueryable<ServiceCategory> ServiceCategories { get; }
        IQueryable<Service> Services { get; }
        IQueryable<Product> Products { get; }
        IQueryable<Invoice> Invoices { get; }
        IQueryable<InvoiceItem> InvoiceItems { get; }
        IQueryable<Payment> Payments { get; }
        IQueryable<InventoryTransaction> InventoryTransactions { get; }
        IQueryable<User> Users { get; }

        void Add<TEntity>(TEntity entity) where TEntity : class;
        void Remove<TEntity>(TEntity entity) where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}