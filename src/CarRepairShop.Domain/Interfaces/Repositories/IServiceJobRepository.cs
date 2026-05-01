using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface IServiceJobRepository : IRepository<ServiceJob>
{
    Task<ServiceJob?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceJob?> GetByIdWithServiceOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<ServiceJob> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceJob, bool>>>? filters = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceJobStatusHistory>> GetHistoryAsync(Guid serviceJobId, CancellationToken cancellationToken = default);
}
