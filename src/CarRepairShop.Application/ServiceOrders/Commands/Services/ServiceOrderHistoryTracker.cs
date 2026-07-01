using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public class ServiceOrderHistoryTracker : IServiceOrderHistoryTracker
{
    private readonly IServiceStatusHistoryRepository _repository;

    public ServiceOrderHistoryTracker(IServiceStatusHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task AddLatestAsync(ServiceOrder order, int previousCount, CancellationToken cancellationToken)
    {
        if (order.StatusHistory.Count <= previousCount)
            return;

        await _repository.AddAsync(order.StatusHistory.Last(), cancellationToken);
    }
}
