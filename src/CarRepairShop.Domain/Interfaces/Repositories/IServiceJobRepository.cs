using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface IServiceJobRepository : IRepository<ServiceJob>
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<(IEnumerable<ServiceJob> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceJob, bool>>>? filters = null,
        CancellationToken cancellationToken = default);
}
