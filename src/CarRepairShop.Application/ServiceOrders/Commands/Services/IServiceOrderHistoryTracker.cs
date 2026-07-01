using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

/// <summary>
/// Persists the latest <see cref="ServiceStatusHistory"/> entry added by a domain
/// operation.  Injected as a scoped service so handlers remain testable without
/// calling into a static helper that references a repository directly.
/// </summary>
public interface IServiceOrderHistoryTracker
{
    /// <summary>
    /// If the order's history grew since <paramref name="previousCount"/>, persists
    /// the new tail entry via the repository.
    /// </summary>
    Task AddLatestAsync(ServiceOrder order, int previousCount, CancellationToken cancellationToken);
}
