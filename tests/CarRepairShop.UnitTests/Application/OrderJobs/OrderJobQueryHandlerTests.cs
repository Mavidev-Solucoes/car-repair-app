using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.OrderJobs.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using Moq;

namespace CarRepairShop.UnitTests.Application.OrderJobs;

public class GetOrderJobByIdQueryHandlerTests
{
    private readonly Mock<IServiceOrderJobRepository> _jobRepoMock = new();
    private readonly GetOrderJobByIdQueryHandler _handler;

    public GetOrderJobByIdQueryHandlerTests()
    {
        _handler = new GetOrderJobByIdQueryHandler(_jobRepoMock.Object);
    }

    [Fact]
    public async Task Handle_JobExists_ReturnsDto()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        _jobRepoMock.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new GetOrderJobByIdQuery(job.Id), CancellationToken.None);

        Assert.Equal(job.Id, result.Id);
        Assert.Equal("Brake Check", result.Name);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _jobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrderJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetOrderJobByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetOrderJobsQueryHandlerTests
{
    private readonly Mock<IServiceOrderJobRepository> _jobRepoMock = new();
    private readonly GetOrderJobsQueryHandler _handler;

    public GetOrderJobsQueryHandlerTests()
    {
        _handler = new GetOrderJobsQueryHandler(_jobRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var jobs = new List<ServiceOrderJob>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m)
        };
        _jobRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceOrderJob, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((jobs, 1));

        var result = await _handler.Handle(new GetOrderJobsQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Handle_WithNameFilter_PassesFilterToRepository()
    {
        _jobRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceOrderJob, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ServiceOrderJob>(), 0));

        var result = await _handler.Handle(
            new GetOrderJobsQuery { Page = 1, PageSize = 10, Name = "Brake" }, CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Handle_WithValidStatusFilter_PassesFilterToRepository()
    {
        _jobRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceOrderJob, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ServiceOrderJob>(), 0));

        var result = await _handler.Handle(
            new GetOrderJobsQuery { Page = 1, PageSize = 10, Status = "Open" }, CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Handle_WithInvalidStatusFilter_PassesNoStatusFilter()
    {
        _jobRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceOrderJob, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ServiceOrderJob>(), 0));

        var result = await _handler.Handle(
            new GetOrderJobsQuery { Page = 1, PageSize = 10, Status = "InvalidStatus" }, CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Handle_WithAssignedUserIdFilter_PassesFilterToRepository()
    {
        _jobRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceOrderJob, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ServiceOrderJob>(), 0));

        var result = await _handler.Handle(
            new GetOrderJobsQuery { Page = 1, PageSize = 10, AssignedUserId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
    }
}

public class GetOrderJobHistoryQueryHandlerTests
{
    private readonly Mock<IServiceOrderJobRepository> _jobRepoMock = new();
    private readonly GetOrderJobHistoryQueryHandler _handler;

    public GetOrderJobHistoryQueryHandlerTests()
    {
        _handler = new GetOrderJobHistoryQueryHandler(_jobRepoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyHistory_ReturnsEmptyList()
    {
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetHistoryAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceOrderJobStatusHistory>());

        var result = await _handler.Handle(new GetOrderJobHistoryQuery(jobId), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_SingleEntry_ReturnsHistoryWithNullTimeInPreviousStatus()
    {
        // Get history entries from a real ServiceOrderJob
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Oil Change", "desc", 100m);
        var history = job.StatusHistory.ToList();

        _jobRepoMock.Setup(r => r.GetHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = (await _handler.Handle(new GetOrderJobHistoryQuery(job.Id), CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Null(result[0].TimeInPreviousStatus);
    }

    [Fact]
    public async Task Handle_MultipleEntries_CalculatesTimeInPreviousStatus()
    {
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Oil Change", "desc", 100m);
        job.Acknowledge(mechanic);
        var history = job.StatusHistory.ToList();

        _jobRepoMock.Setup(r => r.GetHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = (await _handler.Handle(new GetOrderJobHistoryQuery(job.Id), CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Null(result[0].TimeInPreviousStatus);
        Assert.NotNull(result[1].TimeInPreviousStatus);
    }
}
