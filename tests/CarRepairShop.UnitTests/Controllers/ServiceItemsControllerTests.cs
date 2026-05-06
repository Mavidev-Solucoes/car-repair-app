using CarRepairShop.API.Controllers;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceItems.Commands;
using CarRepairShop.Application.ServiceItems.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class ServiceItemsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ServiceItemsController _controller;

    public ServiceItemsControllerTests()
    {
        _controller = new ServiceItemsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var expected = new PagedResult<ServiceItemDto>([], 0, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetServiceItemsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetServiceItemsQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithDto()
    {
        var id = Guid.NewGuid();
        var dto = new ServiceItemDto(id, "Oil Change", "Change oil", 150m, 10, DateTime.UtcNow);
        _mediatorMock.Setup(m => m.Send(It.Is<GetServiceItemByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var command = new CreateServiceItemCommand("Oil Change", "Change oil", 150m, 10);
        var dto = new ServiceItemDto(Guid.NewGuid(), "Oil Change", "Change oil", 150m, 10, DateTime.UtcNow);
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
        var command = new UpdateServiceItemCommand(id, "Oil Change", "Change oil", 160m, 10);
        var dto = new ServiceItemDto(id, "Oil Change", "Change oil", 160m, 10, DateTime.UtcNow);
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
        var command = new UpdateServiceItemCommand(Guid.NewGuid(), "Oil Change", "Change oil", 160m, 10);

        var result = await _controller.Update(routeId, command, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _mediatorMock.Verify(m => m.Send(It.IsAny<IRequest<ServiceItemDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.Is<DeleteServiceItemCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
