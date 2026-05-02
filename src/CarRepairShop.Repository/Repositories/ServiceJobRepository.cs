using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class ServiceJobRepository : Repository<ServiceJob>, IServiceJobRepository
{
    public ServiceJobRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(job => job.Name == name, cancellationToken);
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
            "name" => orderDescending ? query.OrderByDescending(job => job.Name) : query.OrderBy(job => job.Name),
            "price" => orderDescending ? query.OrderByDescending(job => job.Price) : query.OrderBy(job => job.Price),
            "createdat" => orderDescending ? query.OrderByDescending(job => job.CreatedAt) : query.OrderBy(job => job.CreatedAt),
            _ => query.OrderBy(job => job.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
