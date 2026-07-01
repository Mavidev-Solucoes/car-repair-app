using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;

namespace CarRepairShop.Application.OrderJobs.Commands;

public class OrderJobHistoryTracker : IOrderJobHistoryTracker
{
    private readonly IServiceOrderJobStatusHistoryRepository _repository;

    public OrderJobHistoryTracker(IServiceOrderJobStatusHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task AddLatestAsync(ServiceOrderJob serviceJob, int previousCount, CancellationToken cancellationToken)
    {
        if (serviceJob.StatusHistory.Count <= previousCount)
            return;

        await _repository.AddAsync(serviceJob.StatusHistory.Last(), cancellationToken);
    }
}
