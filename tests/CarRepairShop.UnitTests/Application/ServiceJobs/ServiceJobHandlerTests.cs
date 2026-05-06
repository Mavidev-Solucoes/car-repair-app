using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceJobs.Commands;
using CarRepairShop.Application.ServiceJobs.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceJobs;

public class CreateServiceJobCommandHandlerTests
{
    private readonly Mock<IServiceJobRepository> _serviceJobRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly CreateServiceJobCommandHandler _handler;

    public CreateServiceJobCommandHandlerTests()
    {
        _handler = new CreateServiceJobCommandHandler(
            _serviceJobRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesServiceJobAndReturnsDto()
    {
        _serviceJobRepoMock.Setup(r => r.ExistsByNameAsync("Brake Repair", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateServiceJobCommand("Brake Repair", "Fix brakes", 3000m);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Brake Repair", result.Name);
        Assert.Equal(3000m, result.Price);
        _serviceJobRepoMock.Verify(r => r.AddAsync(It.IsAny<ServiceJob>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsBusinessException()
    {
        _serviceJobRepoMock.Setup(r => r.ExistsByNameAsync("Brake Repair", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new CreateServiceJobCommand("Brake Repair", "Fix brakes", 3000m), CancellationToken.None));
    }
}

public class UpdateServiceJobCommandHandlerTests
{
    private readonly Mock<IServiceJobRepository> _serviceJobRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly UpdateServiceJobCommandHandler _handler;

    public UpdateServiceJobCommandHandlerTests()
    {
        _handler = new UpdateServiceJobCommandHandler(
            _serviceJobRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesAndReturnsDto()
    {
        var job = new ServiceJob("Brake Repair", "Fix brakes", 3000m);
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new UpdateServiceJobCommand(job.Id, "Brake Repair Updated", "Fix brakes v2", 3500m);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Brake Repair Updated", result.Name);
        Assert.Equal(3500m, result.Price);
        _serviceJobRepoMock.Verify(r => r.Update(job), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateServiceJobCommand(Guid.NewGuid(), "Name", "Desc", 100m), CancellationToken.None));
    }
}

public class DeleteServiceJobCommandHandlerTests
{
    private readonly Mock<IServiceJobRepository> _serviceJobRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteServiceJobCommandHandler _handler;

    public DeleteServiceJobCommandHandlerTests()
    {
        _handler = new DeleteServiceJobCommandHandler(_serviceJobRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidJob_DeletesAndReturnsUnit()
    {
        var job = new ServiceJob("Brake Repair", "Fix brakes", 3000m);
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new DeleteServiceJobCommand(job.Id), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        _serviceJobRepoMock.Verify(r => r.Delete(job), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteServiceJobCommand(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetServiceJobByIdQueryHandlerTests
{
    private readonly Mock<IServiceJobRepository> _serviceJobRepoMock = new();
    private readonly GetServiceJobByIdQueryHandler _handler;

    public GetServiceJobByIdQueryHandlerTests()
    {
        _handler = new GetServiceJobByIdQueryHandler(_serviceJobRepoMock.Object);
    }

    [Fact]
    public async Task Handle_JobExists_ReturnsDto()
    {
        var job = new ServiceJob("Brake Repair", "Fix brakes", 3000m);
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _serviceJobRepoMock.Setup(r => r.GetAverageTimesInProgressAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, TimeSpan?>());

        var result = await _handler.Handle(new GetServiceJobByIdQuery(job.Id), CancellationToken.None);

        Assert.Equal(job.Id, result.Id);
        Assert.Equal("Brake Repair", result.Name);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetServiceJobByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetServiceJobsQueryHandlerTests
{
    private readonly Mock<IServiceJobRepository> _serviceJobRepoMock = new();
    private readonly GetServiceJobsQueryHandler _handler;

    public GetServiceJobsQueryHandlerTests()
    {
        _handler = new GetServiceJobsQueryHandler(_serviceJobRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var jobs = new List<ServiceJob> { new("Brake Repair", "Fix brakes", 3000m) };
        _serviceJobRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((jobs, 1));
        _serviceJobRepoMock.Setup(r => r.GetAverageTimesInProgressAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, TimeSpan?>());

        var result = await _handler.Handle(new GetServiceJobsQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
