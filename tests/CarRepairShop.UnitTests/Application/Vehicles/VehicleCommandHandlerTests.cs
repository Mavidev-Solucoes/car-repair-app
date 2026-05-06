using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Vehicles.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using Moq;

namespace CarRepairShop.UnitTests.Application.Vehicles;

public class CreateVehicleCommandHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly CreateVehicleCommandHandler _handler;

    public CreateVehicleCommandHandlerTests()
    {
        _handler = new CreateVehicleCommandHandler(
            _vehicleRepoMock.Object,
            _customerRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesVehicleAndReturnsDto()
    {
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _customerRepoMock.Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _vehicleRepoMock.Setup(r => r.ExistsByLicensePlateAsync("ABC1D23", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _currentUserMock.Setup(s => s.UserId).Returns(userId);

        var command = new CreateVehicleCommand(customerId, "Toyota", "Corolla", 2022, "ABC1D23", "White");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal("Toyota", result.Brand);
        Assert.Equal("Corolla", result.Model);
        Assert.Equal(2022, result.Year);
        _vehicleRepoMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new CreateVehicleCommand(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateLicensePlate_ThrowsBusinessException()
    {
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _vehicleRepoMock.Setup(r => r.ExistsByLicensePlateAsync("ABC1D23", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new CreateVehicleCommand(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", null), CancellationToken.None));
    }
}

public class UpdateVehicleCommandHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly UpdateVehicleCommandHandler _handler;

    public UpdateVehicleCommandHandlerTests()
    {
        _handler = new UpdateVehicleCommandHandler(_vehicleRepoMock.Object, _uowMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesVehicle()
    {
        var customerId = Guid.NewGuid();
        var vehicle = new Vehicle(customerId, "Toyota", "Corolla", 2022, "ABC1D23", "White");
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var command = new UpdateVehicleCommand(vehicle.Id, "Toyota", "Camry", 2023, "ABC1D23", "Black");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Camry", result.Model);
        _vehicleRepoMock.Verify(r => r.Update(vehicle), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VehicleNotFound_ThrowsNotFoundException()
    {
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateVehicleCommand(Guid.NewGuid(), "Toyota", "Camry", 2023, "XYZ1234", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_LicensePlateChangedToDuplicate_ThrowsBusinessException()
    {
        var customerId = Guid.NewGuid();
        var vehicle = new Vehicle(customerId, "Toyota", "Corolla", 2022, "ABC1D23", "White");
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _vehicleRepoMock.Setup(r => r.ExistsByLicensePlateAsync("XYZ1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new UpdateVehicleCommand(vehicle.Id, "Toyota", "Corolla", 2022, "XYZ1234", null), CancellationToken.None));
    }
}

public class DeleteVehicleCommandHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteVehicleCommandHandler _handler;

    public DeleteVehicleCommandHandlerTests()
    {
        _handler = new DeleteVehicleCommandHandler(_vehicleRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_VehicleWithNoOrders_DeletesAndReturnsUnit()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White");
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _vehicleRepoMock.Setup(r => r.HasServiceOrdersAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new DeleteVehicleCommand(vehicle.Id), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        _vehicleRepoMock.Verify(r => r.Delete(vehicle), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VehicleNotFound_ThrowsNotFoundException()
    {
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteVehicleCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_VehicleHasServiceOrders_ThrowsBusinessException()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White");
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _vehicleRepoMock.Setup(r => r.HasServiceOrdersAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new DeleteVehicleCommand(vehicle.Id), CancellationToken.None));
    }
}
