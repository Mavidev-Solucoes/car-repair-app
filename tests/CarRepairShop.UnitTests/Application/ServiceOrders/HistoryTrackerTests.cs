using CarRepairShop.Application.OrderJobs.Commands;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceOrders;

public class ServiceOrderHistoryTrackerTests
{
    private readonly Mock<IServiceStatusHistoryRepository> _repoMock = new();
    private readonly ServiceOrderHistoryTracker _tracker;

    public ServiceOrderHistoryTrackerTests()
    {
        _tracker = new ServiceOrderHistoryTracker(_repoMock.Object);
    }

    [Fact]
    public async Task AddLatestAsync_HistoryGrew_AddsLastEntry()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        // StatusHistory starts with 1 entry (CreateInitial)
        var previousCount = 0;

        await _tracker.AddLatestAsync(order, previousCount, CancellationToken.None);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<ServiceStatusHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddLatestAsync_HistoryDidNotGrow_DoesNotAdd()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        // StatusHistory has 1 entry; previousCount is also 1 → no growth
        var previousCount = order.StatusHistory.Count;

        await _tracker.AddLatestAsync(order, previousCount, CancellationToken.None);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<ServiceStatusHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class OrderJobHistoryTrackerTests
{
    private readonly Mock<IServiceOrderJobStatusHistoryRepository> _repoMock = new();
    private readonly OrderJobHistoryTracker _tracker;

    public OrderJobHistoryTrackerTests()
    {
        _tracker = new OrderJobHistoryTracker(_repoMock.Object);
    }

    [Fact]
    public async Task AddLatestAsync_HistoryGrew_AddsLastEntry()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Oil Change", "Change oil", 100m);
        // StatusHistory starts with 1 entry (CreateInitial)
        var previousCount = 0;

        await _tracker.AddLatestAsync(job, previousCount, CancellationToken.None);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<ServiceOrderJobStatusHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddLatestAsync_HistoryDidNotGrow_DoesNotAdd()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Oil Change", "Change oil", 100m);
        var previousCount = job.StatusHistory.Count;

        await _tracker.AddLatestAsync(job, previousCount, CancellationToken.None);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<ServiceOrderJobStatusHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddLatestAsync_AfterAcknowledge_AddsNewEntry()
    {
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 200m);
        var previousCount = job.StatusHistory.Count;
        job.Acknowledge(mechanic);

        await _tracker.AddLatestAsync(job, previousCount, CancellationToken.None);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<ServiceOrderJobStatusHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
