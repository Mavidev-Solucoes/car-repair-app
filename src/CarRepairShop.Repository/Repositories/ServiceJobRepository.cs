using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
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

    public async Task<Dictionary<Guid, TimeSpan?>> GetAverageTimesInProgressAsync(
        IEnumerable<Guid> serviceJobIds,
        CancellationToken cancellationToken = default)
    {
        var ids = serviceJobIds.ToList();
        var result = ids.ToDictionary(id => id, _ => (TimeSpan?)null);

        if (ids.Count == 0)
            return result;

        var serviceOrderJobs = await _context.ServiceOrderJobs
            .Where(j => ids.Contains(j.ServiceJobId))
            .Select(j => new { j.Id, j.ServiceJobId })
            .ToListAsync(cancellationToken);

        if (serviceOrderJobs.Count == 0)
            return result;

        var serviceOrderJobIds = serviceOrderJobs.Select(j => j.Id).ToList();

        var history = await _context.ServiceOrderJobStatusHistory
            .Where(h => serviceOrderJobIds.Contains(h.ServiceOrderJobId) &&
                        (h.ToStatus == JobStatus.InProgress ||
                         (h.FromStatus == JobStatus.InProgress && h.ToStatus == JobStatus.Completed)))
            .Select(h => new { h.ServiceOrderJobId, h.FromStatus, h.ToStatus, h.ChangedAt })
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

        var historyByJobId = history
            .GroupBy(h => h.ServiceOrderJobId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var serviceJobId in ids)
        {
            var jobIds = serviceOrderJobs
                .Where(j => j.ServiceJobId == serviceJobId)
                .Select(j => j.Id)
                .ToHashSet();

            var durations = new List<TimeSpan>();

            foreach (var jobId in jobIds)
            {
                if (!historyByJobId.TryGetValue(jobId, out var jobHistory))
                    continue;

                var inProgressEntry = jobHistory.FirstOrDefault(h => h.ToStatus == JobStatus.InProgress);
                var completedEntry = jobHistory.FirstOrDefault(h =>
                    h.FromStatus == JobStatus.InProgress && h.ToStatus == JobStatus.Completed);

                if (inProgressEntry != null && completedEntry != null)
                    durations.Add(completedEntry.ChangedAt - inProgressEntry.ChangedAt);
            }

            if (durations.Count > 0)
                result[serviceJobId] = TimeSpan.FromSeconds(durations.Average(d => d.TotalSeconds));
        }

        return result;
    }
}
