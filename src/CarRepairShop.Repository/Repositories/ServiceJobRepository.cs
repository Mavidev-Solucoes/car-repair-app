using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class ServiceJobRepository : Repository<ServiceJob>, IServiceJobRepository
{
    public ServiceJobRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<ServiceJob?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(sj => sj.StatusHistory.OrderBy(h => h.ChangedAt))
            .Include(sj => sj.AssignedUser)
            .FirstOrDefaultAsync(sj => sj.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<ServiceJob> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceJob, bool>>>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (filters != null)
            foreach (var filter in filters)
                query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        query = orderBy?.ToLowerInvariant() switch
        {
            "name" => orderDescending ? query.OrderByDescending(sj => sj.Name) : query.OrderBy(sj => sj.Name),
            "unitcost" => orderDescending ? query.OrderByDescending(sj => sj.UnitCost) : query.OrderBy(sj => sj.UnitCost),
            "status" => orderDescending ? query.OrderByDescending(sj => sj.Status) : query.OrderBy(sj => sj.Status),
            "createdat" => orderDescending ? query.OrderByDescending(sj => sj.CreatedAt) : query.OrderBy(sj => sj.CreatedAt),
            _ => query.OrderBy(sj => sj.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<ServiceJobStatusHistory>> GetHistoryAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceJobStatusHistory
            .Where(h => h.ServiceJobId == serviceJobId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }
}
