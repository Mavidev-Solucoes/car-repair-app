using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.OrderJobs.Queries;
using CarRepairShop.Domain.Entities;
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
}
