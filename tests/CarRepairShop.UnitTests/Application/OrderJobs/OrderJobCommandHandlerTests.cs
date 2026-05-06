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

    [Fact]
    public async Task Handle_ValidMechanic_StartsProgressAndReturnsDto()
    {
        var executingOrder = CreateExecutingOrder();
        var job = new ServiceOrderJob(executingOrder.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        // Acknowledge first so the job is in Acknowledged state assigned to mechanic
        job.Acknowledge(mechanic);

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executingOrder);
        _currentUserMock.Setup(s => s.UserId).Returns(mechanic.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(mechanic.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mechanic);

        var result = await _handler.Handle(new StartOrderJobProgressCommand(job.Id), CancellationToken.None);

        Assert.Equal("InProgress", result.Status);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotEmployee_ThrowsBusinessException()
    {
        var executingOrder = CreateExecutingOrder();
        var job = new ServiceOrderJob(executingOrder.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var nonEmployeeUserId = Guid.NewGuid();
        var customer = new Customer("Cust", "52998224725", "c@c.com", "11987654321", "hash");

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executingOrder);
        _currentUserMock.Setup(s => s.UserId).Returns(nonEmployeeUserId);
        _userRepoMock.Setup(r => r.GetByIdAsync(nonEmployeeUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new StartOrderJobProgressCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserIsReceptionist_ThrowsBusinessException()
    {
        var executingOrder = CreateExecutingOrder();
        var job = new ServiceOrderJob(executingOrder.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var receptionist = new Employee("Rec", "r@r.com", "hash", UserRole.Receptionist);

        _jobRepoMock.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _orderRepoMock.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executingOrder);
        _currentUserMock.Setup(s => s.UserId).Returns(receptionist.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(receptionist.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(receptionist);

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

public class CompleteOrderJobCommandHandlerTests
{
    private readonly Mock<IServiceOrderJobRepository> _jobRepoMock = new();
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceOrderJobStatusHistoryRepository> _jobHistoryRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _orderHistoryRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<ILogger<CompleteOrderJobCommandHandler>> _loggerMock = new();
    private readonly CompleteOrderJobCommandHandler _handler;

    public CompleteOrderJobCommandHandlerTests()
    {
        _handler = new CompleteOrderJobCommandHandler(
            _jobRepoMock.Object,
            _orderRepoMock.Object,
            _jobHistoryRepoMock.Object,
            _orderHistoryRepoMock.Object,
            _customerRepoMock.Object,
            _userRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object,
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrderJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new CompleteOrderJobCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotExecuting_ThrowsBusinessException()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var job = CreateJobWithServiceOrder(order);

        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new CompleteOrderJobCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        var order = CreateExecutingOrder();
        var job = CreateJobWithServiceOrder(order);

        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new CompleteOrderJobCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserNotEmployee_ThrowsBusinessException()
    {
        var order = CreateExecutingOrder();
        var job = CreateJobWithServiceOrder(order);
        var nonEmployeeId = Guid.NewGuid();
        var customer = new Customer("Cust", "52998224725", "c@c.com", "11987654321", "hash");

        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserMock.Setup(s => s.UserId).Returns(nonEmployeeId);
        _userRepoMock.Setup(r => r.GetByIdAsync(nonEmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new CompleteOrderJobCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserIsReceptionist_ThrowsBusinessException()
    {
        var order = CreateExecutingOrder();
        var job = CreateJobWithServiceOrder(order);
        var receptionist = new Employee("Rec", "r@r.com", "hash", UserRole.Receptionist);

        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserMock.Setup(s => s.UserId).Returns(receptionist.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(receptionist.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(receptionist);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new CompleteOrderJobCommand(job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidMechanic_CompletesJobAndReturnsDto()
    {
        var order = CreateExecutingOrder();
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        // Create a job in InProgress state assigned to the mechanic
        var job = CreateInProgressJob(order, mechanic);

        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserMock.Setup(s => s.UserId).Returns(mechanic.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(mechanic.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mechanic);

        var result = await _handler.Handle(new CompleteOrderJobCommand(job.Id), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AllJobsCompleted_SendsFinishEmail()
    {
        var order = CreateExecutingOrder();
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = CreateInProgressJob(order, mechanic);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");

        // Attach the job to the order so TryFinish sees all completed
        var orderJobsProp = typeof(ServiceOrder).GetField("_serviceJobs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        (orderJobsProp?.GetValue(order) as List<ServiceOrderJob>)?.Add(job);

        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserMock.Setup(s => s.UserId).Returns(mechanic.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(mechanic.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mechanic);
        _customerRepoMock.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _emailTemplateServiceMock.Setup(t => t.RenderServiceFinishedAsync(It.IsAny<ServiceOrder>(), customer))
            .ReturnsAsync("<html>done</html>");

        var result = await _handler.Handle(new CompleteOrderJobCommand(job.Id), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        _emailServiceMock.Verify(e => e.SendAsync(customer.Email, customer.Name,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailSendFails_StillReturnsDto()
    {
        var order = CreateExecutingOrder();
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = CreateInProgressJob(order, mechanic);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");

        var orderJobsProp = typeof(ServiceOrder).GetField("_serviceJobs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        (orderJobsProp?.GetValue(order) as List<ServiceOrderJob>)?.Add(job);

        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserMock.Setup(s => s.UserId).Returns(mechanic.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(mechanic.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mechanic);
        _customerRepoMock.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _emailTemplateServiceMock.Setup(t => t.RenderServiceFinishedAsync(It.IsAny<ServiceOrder>(), customer))
            .ThrowsAsync(new InvalidOperationException("email error"));

        var result = await _handler.Handle(new CompleteOrderJobCommand(job.Id), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task Handle_AllJobsCompleted_CustomerNull_DoesNotSendEmail()
    {
        var order = CreateExecutingOrder();
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = CreateInProgressJob(order, mechanic);

        var orderJobsProp = typeof(ServiceOrder).GetField("_serviceJobs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        (orderJobsProp?.GetValue(order) as List<ServiceOrderJob>)?.Add(job);

        _jobRepoMock.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserMock.Setup(s => s.UserId).Returns(mechanic.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(mechanic.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mechanic);
        _customerRepoMock.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _handler.Handle(new CompleteOrderJobCommand(job.Id), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        _emailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ServiceOrder CreateExecutingOrder()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var statusProp = typeof(ServiceOrder).GetProperty("Status",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        statusProp?.SetValue(order, ServiceStatus.Executing);
        return order;
    }

    private static ServiceOrderJob CreateJobWithServiceOrder(ServiceOrder order)
    {
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Oil Change", "Change oil", 100m);
        var serviceOrderProp = typeof(ServiceOrderJob).GetProperty("ServiceOrder",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        serviceOrderProp?.SetValue(job, order);
        return job;
    }

    private static ServiceOrderJob CreateInProgressJob(ServiceOrder order, Employee mechanic)
    {
        var job = CreateJobWithServiceOrder(order);
        job.Acknowledge(mechanic);
        job.StartProgress(mechanic.Id);
        return job;
    }
}
