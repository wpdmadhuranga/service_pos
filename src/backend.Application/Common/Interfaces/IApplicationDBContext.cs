using backend.Domain.Entities;
using InventoryItem = backend.Domain.Entities.Inventory.InventoryItem;
using InvoiceItemInventoryUsage = backend.Domain.Entities.Inventory.InvoiceItemInventoryUsage;

namespace backend.Application.Common.Interfaces
{
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
        IQueryable<InventoryItem> InventoryItems { get; }  
        IQueryable<InvoiceItemInventoryUsage> InvoiceItemInventoryUsages { get; }

        void Add<TEntity>(TEntity entity) where TEntity : class;
        void Remove<TEntity>(TEntity entity) where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}