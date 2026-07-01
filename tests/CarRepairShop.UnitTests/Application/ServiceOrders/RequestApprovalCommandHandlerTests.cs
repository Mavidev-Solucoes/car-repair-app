using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceOrders;

public class RequestApprovalCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToApprovalRequestService()
    {
        var command = new RequestApprovalCommand(Guid.NewGuid());
        var expected = new ServiceOrderDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "WaitingForApproval", 0m, DateTime.UtcNow,
            Enumerable.Empty<ServiceOrderItemDto>(), Enumerable.Empty<ServiceOrderJobDto>(), Enumerable.Empty<ServiceStatusHistoryDto>());

        var approvalServiceMock = new Mock<IServiceOrderApprovalRequestService>();
        approvalServiceMock.Setup(s => s.RequestApprovalAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var handler = new RequestApprovalCommandHandler(approvalServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(expected, result);
    }
}

public class ServiceOrderApprovalRequestServiceTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IServiceOrderNotificationService> _notificationServiceMock = new();
    private readonly ServiceOrderApprovalRequestService _service;

    public ServiceOrderApprovalRequestServiceTests()
    {
        _service = new ServiceOrderApprovalRequestService(
            _orderRepoMock.Object,
            _customerRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object,
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task RequestApprovalAsync_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.RequestApprovalAsync(new RequestApprovalCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RequestApprovalAsync_OrderNotFound_ThrowsNotFoundException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.RequestApprovalAsync(new RequestApprovalCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RequestApprovalAsync_ValidOrder_RequestsApprovalAndReturnsDto()
    {
        var employeeId = Guid.NewGuid();
        var order = CreateDiagnosingOrderWithAcknowledgedJob(employeeId);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");

        _currentUserMock.Setup(s => s.UserId).Returns(employeeId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _customerRepoMock.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _service.RequestApprovalAsync(new RequestApprovalCommand(order.Id), CancellationToken.None);

        Assert.Equal("WaitingForApproval", result.Status);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(n => n.NotifyApprovalRequestedAsync(order, customer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestApprovalAsync_CustomerNotFound_ThrowsNotFoundException()
    {
        var employeeId = Guid.NewGuid();
        var order = CreateDiagnosingOrderWithAcknowledgedJob(employeeId);

        _currentUserMock.Setup(s => s.UserId).Returns(employeeId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.RequestApprovalAsync(new RequestApprovalCommand(order.Id), CancellationToken.None));
    }

    private static ServiceOrder CreateDiagnosingOrderWithAcknowledgedJob(Guid employeeId)
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Oil Change", "Change oil", 100m, employeeId);
        order.AttachServiceJob(job, employeeId);
        job.Acknowledge(mechanic);
        return order;
    }
}
