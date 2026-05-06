using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceOrders.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using CarRepairShop.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceOrders;

public class OpenServiceCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<ILogger<OpenServiceCommandHandler>> _loggerMock = new();
    private readonly OpenServiceCommandHandler _handler;

    public OpenServiceCommandHandlerTests()
    {
        _handler = new OpenServiceCommandHandler(
            _orderRepoMock.Object,
            _vehicleRepoMock.Object,
            _customerRepoMock.Object,
            _userRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object,
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrderAndReturnsDto()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");

        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _emailTemplateServiceMock.Setup(t => t.RenderServiceReceivedAsync(
                It.IsAny<ServiceOrder>(), customer, vehicle, employee))
            .ReturnsAsync("<html>email</html>");

        var command = new OpenServiceCommand(vehicle.Id, customer.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(vehicle.Id, result.VehicleId);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(employee.Id, result.AssignedUserId);
        Assert.Equal("Received", result.Status);
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonEmployeeUser_ThrowsBusinessException()
    {
        var userId = Guid.NewGuid();
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_VehicleNotFound_ThrowsNotFoundException()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White");
        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new OpenServiceCommand(vehicle.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailServiceFails_StillReturnsDto()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");

        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _emailTemplateServiceMock.Setup(t => t.RenderServiceReceivedAsync(
                It.IsAny<ServiceOrder>(), customer, vehicle, employee))
            .ThrowsAsync(new InvalidOperationException("Email error"));

        var command = new OpenServiceCommand(vehicle.Id, customer.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        // Should still return a valid DTO even if email fails
        Assert.Equal("Received", result.Status);
    }
}

public class AddServiceItemCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceOrderItemRepository> _orderItemRepoMock = new();
    private readonly Mock<IServiceItemRepository> _serviceItemRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly AddServiceItemCommandHandler _handler;

    public AddServiceItemCommandHandlerTests()
    {
        _handler = new AddServiceItemCommandHandler(
            _orderRepoMock.Object,
            _orderItemRepoMock.Object,
            _serviceItemRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new AddServiceItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new AddServiceItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ServiceItemNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new AddServiceItemCommand(order.Id, Guid.NewGuid(), 1), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsItemAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var catalogItem = new ServiceItem("Oil Change", "Change oil", 150m, 10, userId);
        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(catalogItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalogItem);

        var result = await _handler.Handle(new AddServiceItemCommand(order.Id, catalogItem.Id, 2), CancellationToken.None);

        Assert.Equal("Diagnosing", result.Status);
        Assert.Single(result.ServiceItems);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RemoveServiceItemCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly RemoveServiceItemCommandHandler _handler;

    public RemoveServiceItemCommandHandlerTests()
    {
        _handler = new RemoveServiceItemCommandHandler(
            _orderRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new RemoveServiceItemCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RemoveServiceItemCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}

public class DeliverServiceCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly DeliverServiceCommandHandler _handler;

    public DeliverServiceCommandHandlerTests()
    {
        _handler = new DeliverServiceCommandHandler(
            _orderRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new DeliverServiceCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeliverServiceCommand(Guid.NewGuid()), CancellationToken.None));
    }
}

public class DisputeServiceCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly DisputeServiceCommandHandler _handler;

    public DisputeServiceCommandHandlerTests()
    {
        _handler = new DisputeServiceCommandHandler(
            _orderRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new DisputeServiceCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DisputeServiceCommand(Guid.NewGuid()), CancellationToken.None));
    }
}

public class ApproveServiceCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ApproveServiceCommandHandler _handler;

    public ApproveServiceCommandHandlerTests()
    {
        _handler = new ApproveServiceCommandHandler(
            _orderRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new ApproveServiceCommand(Guid.NewGuid()), CancellationToken.None));
    }
}

public class RequestApprovalCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<IOptions<AppSettings>> _appSettingsMock = new();
    private readonly Mock<ILogger<RequestApprovalCommandHandler>> _loggerMock = new();
    private readonly RequestApprovalCommandHandler _handler;

    public RequestApprovalCommandHandlerTests()
    {
        _appSettingsMock.Setup(o => o.Value).Returns(new AppSettings { BaseUrl = "https://example.com" });
        _handler = new RequestApprovalCommandHandler(
            _orderRepoMock.Object,
            _customerRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object,
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _appSettingsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new RequestApprovalCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RequestApprovalCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
