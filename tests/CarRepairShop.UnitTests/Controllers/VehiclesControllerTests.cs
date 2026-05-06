using CarRepairShop.API.Controllers;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.Vehicles.Commands;
using CarRepairShop.Application.Vehicles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class VehiclesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly VehiclesController _controller;

    public VehiclesControllerTests()
    {
        _controller = new VehiclesController(_mediatorMock.Object);
    }

    private static VehicleDto SampleDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White", DateTime.UtcNow);

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var expected = new PagedResult<VehicleDto>([], 0, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetVehiclesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetVehiclesQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<GetVehicleByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetByCustomerId_ReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var vehicles = new List<VehicleDto> { SampleDto() };
        _mediatorMock.Setup(m => m.Send(It.Is<GetVehiclesByCustomerIdQuery>(q => q.CustomerId == customerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicles);

        var result = await _controller.GetByCustomerId(customerId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(vehicles, ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var command = new CreateVehicleCommand(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White");
        var dto = SampleDto();
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Create(command, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetById), created.ActionName);
        Assert.Equal(dto, created.Value);
    }

    [Fact]
    public async Task Update_WithMatchingIds_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var command = new UpdateVehicleCommand(id, "Toyota", "Camry", 2023, "ABC1D23", "Black");
        var dto = SampleDto(id);
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Update(id, command, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Update_WithMismatchedIds_ReturnsBadRequest()
    {
        var routeId = Guid.NewGuid();
        var command = new UpdateVehicleCommand(Guid.NewGuid(), "Toyota", "Camry", 2023, "ABC1D23", "Black");

        var result = await _controller.Update(routeId, command, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _mediatorMock.Verify(m => m.Send(It.IsAny<IRequest<VehicleDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.Is<DeleteVehicleCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
