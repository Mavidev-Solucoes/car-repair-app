using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.OrderJobs.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarRepairShop.UnitTests.Application.OrderJobs;

public class AcknowledgeOrderJobCommandHandlerTests
{
    private readonly Mock<IServiceOrderJobRepository> _jobRepoMock = new();
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceOrderJobStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly AcknowledgeOrderJobCommandHandler _handler;

    public AcknowledgeOrderJobCommandHandlerTests()
    {
        _handler = new AcknowledgeOrderJobCommandHandler(
            _jobRepoMock.Object,
            _orderRepoMock.Object,
            _historyRepoMock.Object,
            _userRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrderJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new AcknowledgeOrderJobCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new AcknowledgeOrderJobCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotInDiagnosing_ThrowsBusinessException()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Order is in Received status, not Diagnosing
        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new AcknowledgeOrderJobCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrderInDiagnosing(userId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new AcknowledgeOrderJobCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserNotMechanicOrAdmin_ThrowsBusinessException()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrderInDiagnosing(userId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechUserId = Guid.NewGuid();
        // Employee with Receptionist role cannot acknowledge
        var advisor = new Employee("Advisor", "a@a.com", "hash", UserRole.Receptionist);

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _currentUserMock.Setup(s => s.UserId).Returns(mechUserId);
        _userRepoMock.Setup(r => r.GetByIdAsync(mechUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(advisor);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new AcknowledgeOrderJobCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidMechanic_AcknowledgesJobAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrderInDiagnosing(userId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechUserId = Guid.NewGuid();
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _currentUserMock.Setup(s => s.UserId).Returns(mechanic.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(mechanic.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mechanic);

        var result = await _handler.Handle(new AcknowledgeOrderJobCommand(job.Id), CancellationToken.None);

        Assert.Equal("Acknowledged", result.Status);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ServiceOrder CreateOrderInDiagnosing(Guid userId)
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var item = new ServiceOrderItem(order.Id, Guid.NewGuid(), "Part", 100m, 1);
        order.AddServiceItem(item, userId);
        return order;
    }
}

public class StartOrderJobProgressCommandHandlerTests
{
    private readonly Mock<IServiceOrderJobRepository> _jobRepoMock = new();
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceOrderJobStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly StartOrderJobProgressCommandHandler _handler;

    public StartOrderJobProgressCommandHandlerTests()
    {
        _handler = new StartOrderJobProgressCommandHandler(
            _jobRepoMock.Object,
            _orderRepoMock.Object,
            _historyRepoMock.Object,
            _userRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrderJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new StartOrderJobProgressCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new StartOrderJobProgressCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotInExecuting_ThrowsBusinessException()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new StartOrderJobProgressCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        // Simulate executing state by mocking the order
        var executingOrder = CreateExecutingOrder();

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executingOrder);
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new StartOrderJobProgressCommand(job.Id), CancellationToken.None));
    }

    private static ServiceOrder CreateExecutingOrder()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        // Use reflection to set the status to Executing for the mock
        var statusProp = typeof(ServiceOrder).GetProperty("Status",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        statusProp?.SetValue(order, ServiceStatus.Executing);
        return order;
    }
}
