using CarRepairShop.API.Controllers;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.Customers.Commands;
using CarRepairShop.Application.Customers.Queries;
using CarRepairShop.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        _controller = new CustomersController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var expected = new PagedResult<CustomerDto>([], 0, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCustomersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetCustomersQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithCustomerDto()
    {
        var id = Guid.NewGuid();
        var dto = new CustomerDto(id, "John", "52998224725", "john@example.com", "11987654321", true, DateTime.UtcNow);
        _mediatorMock.Setup(m => m.Send(It.Is<GetCustomerByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetWithVehicles_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new CustomerDto(id, "John", "52998224725", "john@example.com", "11987654321", true, DateTime.UtcNow);
        _mediatorMock.Setup(m => m.Send(It.Is<GetCustomerWithVehiclesQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetWithVehicles(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var command = new CreateCustomerCommand("John", "52998224725", "john@example.com", "11987654321");
        var dto = new CustomerDto(Guid.NewGuid(), "John", "52998224725", "john@example.com", "11987654321", true, DateTime.UtcNow);
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
        var command = new UpdateCustomerCommand(id, "Jane", "jane@example.com", "11912345678");
        var dto = new CustomerDto(id, "Jane", "52998224725", "jane@example.com", "11912345678", true, DateTime.UtcNow);
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
        var command = new UpdateCustomerCommand(Guid.NewGuid(), "Jane", "jane@example.com", "11912345678");

        var result = await _controller.Update(routeId, command, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _mediatorMock.Verify(m => m.Send(It.IsAny<IRequest<CustomerDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.Is<DeleteCustomerCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
