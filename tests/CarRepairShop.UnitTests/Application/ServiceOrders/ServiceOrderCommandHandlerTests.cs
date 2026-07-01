using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceOrders.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using CarRepairShop.Application.Settings;
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

    [Fact]
    public async Task Handle_ValidOrder_RequestsApprovalAndReturnsDto()
    {
        var employeeId = Guid.NewGuid();
        var order = CreateDiagnosingOrderWithAcknowledgedJob(employeeId);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");

        _currentUserMock.Setup(s => s.UserId).Returns(employeeId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _customerRepoMock.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _emailTemplateServiceMock.Setup(t => t.RenderWaitingForApprovalAsync(
                It.IsAny<ServiceOrder>(), customer, It.IsAny<string>()))
            .ReturnsAsync("<html>approval</html>");

        var result = await _handler.Handle(new RequestApprovalCommand(order.Id), CancellationToken.None);

        Assert.Equal("WaitingForApproval", result.Status);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        var employeeId = Guid.NewGuid();
        var order = CreateDiagnosingOrderWithAcknowledgedJob(employeeId);

        _currentUserMock.Setup(s => s.UserId).Returns(employeeId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RequestApprovalCommand(order.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailFails_StillReturnsDto()
    {
        var employeeId = Guid.NewGuid();
        var order = CreateDiagnosingOrderWithAcknowledgedJob(employeeId);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");

        _currentUserMock.Setup(s => s.UserId).Returns(employeeId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _customerRepoMock.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _emailTemplateServiceMock.Setup(t => t.RenderWaitingForApprovalAsync(
                It.IsAny<ServiceOrder>(), customer, It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("email error"));

        var result = await _handler.Handle(new RequestApprovalCommand(order.Id), CancellationToken.None);

        Assert.Equal("WaitingForApproval", result.Status);
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

public class AddServiceJobCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceJobRepository> _serviceJobRepoMock = new();
    private readonly Mock<IServiceOrderJobRepository> _serviceOrderJobRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly AddServiceJobCommandHandler _handler;

    public AddServiceJobCommandHandlerTests()
    {
        _handler = new AddServiceJobCommandHandler(
            _orderRepoMock.Object,
            _serviceJobRepoMock.Object,
            _serviceOrderJobRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new AddServiceJobCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new AddServiceJobCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ServiceJobNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new AddServiceJobCommand(order.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsJobAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var serviceJob = new ServiceJob("Brake Inspection", "Inspect brakes", 200m, userId);
        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(serviceJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceJob);

        var result = await _handler.Handle(new AddServiceJobCommand(order.Id, serviceJob.Id), CancellationToken.None);

        Assert.Equal("Diagnosing", result.Status);
        Assert.Single(result.ServiceJobs);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RemoveServiceJobCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceOrderJobRepository> _serviceOrderJobRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly RemoveServiceJobCommandHandler _handler;

    public RemoveServiceJobCommandHandlerTests()
    {
        _handler = new RemoveServiceJobCommandHandler(
            _orderRepoMock.Object,
            _serviceOrderJobRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new RemoveServiceJobCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RemoveServiceJobCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_JobNotFoundInRepository_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Oil Change", "Change oil", 100m, userId);
        order.AttachServiceJob(job, userId);

        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _serviceOrderJobRepoMock.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrderJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RemoveServiceJobCommand(order.Id, job.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesJobAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Oil Change", "Change oil", 100m, userId);
        order.AttachServiceJob(job, userId);

        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _serviceOrderJobRepoMock.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new RemoveServiceJobCommand(order.Id, job.Id), CancellationToken.None);

        Assert.NotNull(result);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RemoveServiceItemCommandHandlerSuccessTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly RemoveServiceItemCommandHandler _handler;

    public RemoveServiceItemCommandHandlerSuccessTests()
    {
        _handler = new RemoveServiceItemCommandHandler(
            _orderRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ServiceItemNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RemoveServiceItemCommand(order.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesItemAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var catalogItem = new ServiceItem("Oil", "Oil filter", 50m, 10, userId);
        var orderItem = new ServiceOrderItem(order.Id, catalogItem.Id, "Oil filter", 50m, 1);

        // Attach catalog item reference via reflection
        var serviceItemProp = typeof(ServiceOrderItem).GetProperty("ServiceItem",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        serviceItemProp?.SetValue(orderItem, catalogItem);

        // Add item to order's list via reflection
        var itemsField = typeof(ServiceOrder).GetField("_serviceItems",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        (itemsField?.GetValue(order) as List<ServiceOrderItem>)?.Add(orderItem);

        // Put order in Diagnosing state via reflection
        var statusProp = typeof(ServiceOrder).GetProperty("Status",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        statusProp?.SetValue(order, ServiceStatus.Diagnosing);

        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(new RemoveServiceItemCommand(order.Id, orderItem.Id), CancellationToken.None);

        Assert.NotNull(result);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ApproveServiceCommandHandlerSuccessTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ApproveServiceCommandHandler _handler;

    public ApproveServiceCommandHandlerSuccessTests()
    {
        _handler = new ApproveServiceCommandHandler(
            _orderRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidOrder_ApprovesAndReturnsDto()
    {
        var employeeId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Oil Change", "Change oil", 100m, employeeId);
        order.AttachServiceJob(job, employeeId);
        job.Acknowledge(mechanic);
        order.RequestApproval(employeeId);

        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(new ApproveServiceCommand(order.Id), CancellationToken.None);

        Assert.Equal("Executing", result.Status);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeliverServiceCommandHandlerSuccessTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly DeliverServiceCommandHandler _handler;

    public DeliverServiceCommandHandlerSuccessTests()
    {
        _handler = new DeliverServiceCommandHandler(
            _orderRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidOrder_DeliversAndReturnsDto()
    {
        var employeeId = Guid.NewGuid();
        var order = CreateFinishedOrder(employeeId);

        _currentUserMock.Setup(s => s.UserId).Returns(employeeId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(new DeliverServiceCommand(order.Id), CancellationToken.None);

        Assert.Equal("Delivered", result.Status);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ServiceOrder CreateFinishedOrder(Guid employeeId)
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Job", "desc", 100m, employeeId);
        order.AttachServiceJob(job, employeeId);
        job.Acknowledge(mechanic);
        order.RequestApproval(employeeId);
        order.Approve();
        job.StartProgress(mechanic.Id);
        job.Complete(mechanic.Id);
        order.TryFinish();
        return order;
    }
}

public class DisputeServiceCommandHandlerSuccessTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly DisputeServiceCommandHandler _handler;

    public DisputeServiceCommandHandlerSuccessTests()
    {
        _handler = new DisputeServiceCommandHandler(
            _orderRepoMock.Object,
            _historyRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidOrder_DisputesAndReturnsDto()
    {
        var employeeId = Guid.NewGuid();
        var order = CreateFinishedOrder(employeeId);

        _currentUserMock.Setup(s => s.UserId).Returns(employeeId);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(new DisputeServiceCommand(order.Id), CancellationToken.None);

        Assert.Equal("Diagnosing", result.Status);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ServiceOrder CreateFinishedOrder(Guid employeeId)
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Job", "desc", 100m, employeeId);
        order.AttachServiceJob(job, employeeId);
        job.Acknowledge(mechanic);
        order.RequestApproval(employeeId);
        order.Approve();
        job.StartProgress(mechanic.Id);
        job.Complete(mechanic.Id);
        order.TryFinish();
        return order;
    }
}
