using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class ServiceOrderJobRepository : Repository<ServiceOrderJob>, IServiceOrderJobRepository
{
    public ServiceOrderJobRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<ServiceOrderJob?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(job => job.StatusHistory.OrderBy(h => h.ChangedAt))
            .Include(job => job.AssignedUser)
            .FirstOrDefaultAsync(job => job.Id == id, cancellationToken);
    }

    public async Task<ServiceOrderJob?> GetByIdWithServiceOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(job => job.StatusHistory.OrderBy(h => h.ChangedAt))
            .Include(job => job.AssignedUser)
            .Include(job => job.ServiceJob)
            .Include(job => job.ServiceOrder)
                .ThenInclude(order => order.ServiceJobs)
            .Include(job => job.ServiceOrder)
                .ThenInclude(order => order.ServiceItems)
            .Include(job => job.ServiceOrder)
                .ThenInclude(order => order.StatusHistory.OrderBy(h => h.ChangedAt))
            .Include(job => job.ServiceOrder)
                .ThenInclude(order => order.Vehicle)
            .FirstOrDefaultAsync(job => job.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<ServiceOrderJob> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceOrderJob, bool>>>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(job => job.ServiceOrder)
            .AsQueryable();

        if (filters != null)
            foreach (var filter in filters)
                query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        query = orderBy?.ToLowerInvariant() switch
        {
            "name" => orderDescending ? query.OrderByDescending(job => job.Name) : query.OrderBy(job => job.Name),
            "price" => orderDescending ? query.OrderByDescending(job => job.Price) : query.OrderBy(job => job.Price),
            "status" => orderDescending ? query.OrderByDescending(job => job.Status) : query.OrderBy(job => job.Status),
            "createdat" => orderDescending ? query.OrderByDescending(job => job.CreatedAt) : query.OrderBy(job => job.CreatedAt),
            _ => query.OrderBy(job => job.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<ServiceOrderJobStatusHistory>> GetHistoryAsync(Guid serviceOrderJobId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceOrderJobStatusHistory
            .Where(h => h.ServiceOrderJobId == serviceOrderJobId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }
}
