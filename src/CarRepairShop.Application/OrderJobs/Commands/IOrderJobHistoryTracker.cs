using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Application.OrderJobs.Commands;

/// <summary>
/// Persists the latest <see cref="ServiceOrderJobStatusHistory"/> entry added by a
/// domain operation on a <see cref="ServiceOrderJob"/>.
/// </summary>
public interface IOrderJobHistoryTracker
{
    Task AddLatestAsync(ServiceOrderJob serviceJob, int previousCount, CancellationToken cancellationToken);
}
