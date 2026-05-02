using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface IServiceOrderJobRepository : IRepository<ServiceOrderJob>
{
    Task<ServiceOrderJob?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceOrderJob?> GetByIdWithServiceOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<ServiceOrderJob> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceOrderJob, bool>>>? filters = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceOrderJobStatusHistory>> GetHistoryAsync(Guid serviceOrderJobId, CancellationToken cancellationToken = default);
}
