using CarRepairShop.Application.Common;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Vehicles.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using Moq;

namespace CarRepairShop.UnitTests.Application.Vehicles;

public class GetVehicleByIdQueryHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly GetVehicleByIdQueryHandler _handler;

    public GetVehicleByIdQueryHandlerTests()
    {
        _handler = new GetVehicleByIdQueryHandler(_vehicleRepoMock.Object);
    }

    [Fact]
    public async Task Handle_VehicleExists_ReturnsDto()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White");
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var result = await _handler.Handle(new GetVehicleByIdQuery(vehicle.Id), CancellationToken.None);

        Assert.Equal(vehicle.Id, result.Id);
        Assert.Equal("Toyota", result.Brand);
        Assert.Equal("Corolla", result.Model);
    }

    [Fact]
    public async Task Handle_VehicleNotFound_ThrowsNotFoundException()
    {
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetVehicleByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetVehiclesByCustomerIdQueryHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly GetVehiclesByCustomerIdQueryHandler _handler;

    public GetVehiclesByCustomerIdQueryHandlerTests()
    {
        _handler = new GetVehiclesByCustomerIdQueryHandler(_vehicleRepoMock.Object);
    }

    [Fact]
    public async Task Handle_CustomerHasVehicles_ReturnsAllVehicles()
    {
        var customerId = Guid.NewGuid();
        var vehicles = new List<Vehicle>
        {
            new(customerId, "Toyota", "Corolla", 2022, "ABC1D23", "White"),
            new(customerId, "Honda", "Civic", 2023, "DEF4G56", "Red")
        };
        _vehicleRepoMock.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicles);

        var result = await _handler.Handle(new GetVehiclesByCustomerIdQuery(customerId), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Handle_CustomerHasNoVehicles_ReturnsEmptyList()
    {
        var customerId = Guid.NewGuid();
        _vehicleRepoMock.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetVehiclesByCustomerIdQuery(customerId), CancellationToken.None);

        Assert.Empty(result);
    }
}

public class GetVehiclesQueryHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly GetVehiclesQueryHandler _handler;

    public GetVehiclesQueryHandlerTests()
    {
        _handler = new GetVehiclesQueryHandler(_vehicleRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var vehicles = new List<Vehicle>
        {
            new(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White")
        };
        _vehicleRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<Vehicle, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, 1));

        var result = await _handler.Handle(new GetVehiclesQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
