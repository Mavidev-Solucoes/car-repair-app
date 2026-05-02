using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface IServiceItemRepository : IRepository<ServiceItem>
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid excludingId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<ServiceItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceItem, bool>>>? filters = null,
        CancellationToken cancellationToken = default);
}
