using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class ServiceItemRepository : Repository<ServiceItem>, IServiceItemRepository
{
    public ServiceItemRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(si => si.Name == name, cancellationToken);
    }

    public async Task<(IEnumerable<ServiceItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceItem, bool>>>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (filters != null)
            foreach (var filter in filters)
                query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        query = orderBy?.ToLowerInvariant() switch
        {
            "name" => orderDescending ? query.OrderByDescending(si => si.Name) : query.OrderBy(si => si.Name),
            "unitcost" => orderDescending ? query.OrderByDescending(si => si.UnitCost) : query.OrderBy(si => si.UnitCost),
            "createdat" => orderDescending ? query.OrderByDescending(si => si.CreatedAt) : query.OrderBy(si => si.CreatedAt),
            _ => query.OrderBy(si => si.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
