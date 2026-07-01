using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;

namespace CarRepairShop.Application.ServiceOrders.Commands;

internal static class ServiceOrderHistoryPersistence
{
    internal static async Task AddLatestAsync(
        ServiceOrder order,
        int previousHistoryCount,
        IServiceStatusHistoryRepository serviceStatusHistoryRepository,
        CancellationToken cancellationToken)
    {
        if (order.StatusHistory.Count <= previousHistoryCount)
            return;

        await serviceStatusHistoryRepository.AddAsync(order.StatusHistory.Last(), cancellationToken);
    }
}
