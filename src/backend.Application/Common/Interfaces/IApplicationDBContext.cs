using backend.Domain.Entities;

namespace backend.Application.Common.Interfaces
{
    // Implemented by ApplicationDbContext in Persistence.
    // Lets Application/Infrastructure query the database without referencing
    // Persistence directly - keeps the dependency pointing inward.
    public interface IApplicationDbContext
    {
        IQueryable<User> Users { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}