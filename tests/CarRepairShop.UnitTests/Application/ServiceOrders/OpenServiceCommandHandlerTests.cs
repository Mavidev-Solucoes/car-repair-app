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

public class OpenServiceCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToOpeningService()
    {
        var command = new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid());
        var expected = new ServiceOrderDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Received", 0m, DateTime.UtcNow,
            Enumerable.Empty<ServiceOrderItemDto>(), Enumerable.Empty<ServiceOrderJobDto>(), Enumerable.Empty<ServiceStatusHistoryDto>());

        var openingServiceMock = new Mock<IServiceOrderOpeningService>();
        openingServiceMock.Setup(s => s.OpenAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var handler = new OpenServiceCommandHandler(openingServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(expected, result);
    }
}

public class ServiceOrderOpeningServiceTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IServiceOrderItemRepository> _orderItemRepoMock = new();
    private readonly Mock<IServiceOrderJobRepository> _orderJobRepoMock = new();
    private readonly Mock<IServiceItemRepository> _serviceItemRepoMock = new();
    private readonly Mock<IServiceJobRepository> _serviceJobRepoMock = new();
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IServiceOrderNotificationService> _notificationServiceMock = new();
    private readonly Mock<IServiceOrderBusinessTelemetry> _telemetryMock = new();
    private readonly ServiceOrderOpeningService _service;

    public ServiceOrderOpeningServiceTests()
    {
        _service = new ServiceOrderOpeningService(
            _orderRepoMock.Object,
            _orderItemRepoMock.Object,
            _orderJobRepoMock.Object,
            _serviceItemRepoMock.Object,
            _serviceJobRepoMock.Object,
            _vehicleRepoMock.Object,
            _customerRepoMock.Object,
            _userRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object,
            _notificationServiceMock.Object,
            _telemetryMock.Object);
    }

    [Fact]
    public async Task OpenAsync_ValidCommand_CreatesOrderAndReturnsDto()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");

        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var command = new OpenServiceCommand(vehicle.Id, customer.Id);
        var result = await _service.OpenAsync(command, CancellationToken.None);

        Assert.Equal(vehicle.Id, result.VehicleId);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(employee.Id, result.AssignedUserId);
        Assert.Equal("Received", result.Status);
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _telemetryMock.Verify(t => t.RecordCreatedAsync(It.Is<ServiceOrder>(o => o.Id == result.Id), It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(n => n.NotifyServiceReceivedAsync(It.IsAny<ServiceOrder>(), customer, vehicle, employee, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_WithItems_CreatesOrderWithItemsAndReturnsDto()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");
        var catalogItem = new ServiceItem("Oil Filter", "Oil filter replacement", 30m, 10);

        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(catalogItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalogItem);

        var command = new OpenServiceCommand(vehicle.Id, customer.Id,
            Items: [new OpenServiceItemInput(catalogItem.Id, 2)]);
        var result = await _service.OpenAsync(command, CancellationToken.None);

        Assert.Equal("Diagnosing", result.Status);
        Assert.Single(result.ServiceItems);
        Assert.Equal(60m, result.TotalPrice);
        _orderItemRepoMock.Verify(r => r.AddAsync(It.IsAny<ServiceOrderItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_WithJobs_CreatesOrderWithJobsAndReturnsDto()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");
        var catalogJob = new ServiceJob("Oil Change", "Full oil change", 80m);

        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(catalogJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalogJob);

        var command = new OpenServiceCommand(vehicle.Id, customer.Id,
            Jobs: [new OpenServiceJobInput(catalogJob.Id)]);
        var result = await _service.OpenAsync(command, CancellationToken.None);

        Assert.Equal("Diagnosing", result.Status);
        Assert.Single(result.ServiceJobs);
        Assert.Equal(80m, result.TotalPrice);
        _orderJobRepoMock.Verify(r => r.AddAsync(It.IsAny<ServiceOrderJob>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_WithItemsAndJobs_CreatesFullOrderAndReturnsDto()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");
        var catalogItem = new ServiceItem("Brake Pad", "Brake pad set", 120m, 5);
        var catalogJob = new ServiceJob("Brake Replacement", "Replace brake pads", 150m);

        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(catalogItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalogItem);
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(catalogJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalogJob);

        var command = new OpenServiceCommand(vehicle.Id, customer.Id,
            Items: [new OpenServiceItemInput(catalogItem.Id, 1)],
            Jobs: [new OpenServiceJobInput(catalogJob.Id)]);
        var result = await _service.OpenAsync(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Diagnosing", result.Status);
        Assert.Single(result.ServiceItems);
        Assert.Single(result.ServiceJobs);
        Assert.Equal(270m, result.TotalPrice);
    }

    [Fact]
    public async Task OpenAsync_ItemNotFound_ThrowsNotFoundException()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");

        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceItem?)null);

        var command = new OpenServiceCommand(vehicle.Id, customer.Id,
            Items: [new OpenServiceItemInput(Guid.NewGuid(), 1)]);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.OpenAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task OpenAsync_JobNotFound_ThrowsNotFoundException()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");

        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceJobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceJob?)null);

        var command = new OpenServiceCommand(vehicle.Id, customer.Id,
            Jobs: [new OpenServiceJobInput(Guid.NewGuid())]);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.OpenAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task OpenAsync_NoAuthenticatedUser_ThrowsBusinessException()
    {
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.OpenAsync(new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task OpenAsync_NonEmployeeUser_ThrowsBusinessException()
    {
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.OpenAsync(new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task OpenAsync_VehicleNotFound_ThrowsNotFoundException()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.OpenAsync(new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task OpenAsync_CustomerNotFound_ThrowsNotFoundException()
    {
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White");
        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetEmployeeByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.OpenAsync(new OpenServiceCommand(vehicle.Id, Guid.NewGuid()), CancellationToken.None));
    }
}
