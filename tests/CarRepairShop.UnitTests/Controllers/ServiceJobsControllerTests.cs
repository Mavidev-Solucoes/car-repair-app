using CarRepairShop.API.Controllers;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceJobs.Commands;
using CarRepairShop.Application.ServiceJobs.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class ServiceJobsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ServiceJobsController _controller;

    public ServiceJobsControllerTests()
    {
        _controller = new ServiceJobsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var expected = new PagedResult<ServiceJobDto>([], 0, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetServiceJobsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetServiceJobsQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new ServiceJobDto(id, "Brake Repair", "Fix brakes", 3000m, DateTime.UtcNow);
        _mediatorMock.Setup(m => m.Send(It.Is<GetServiceJobByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var command = new CreateServiceJobCommand("Brake Repair", "Fix brakes", 3000m);
        var dto = new ServiceJobDto(Guid.NewGuid(), "Brake Repair", "Fix brakes", 3000m, DateTime.UtcNow);
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
        var command = new UpdateServiceJobCommand(id, "Brake Repair Updated", "Fix brakes v2", 3500m);
        var dto = new ServiceJobDto(id, "Brake Repair Updated", "Fix brakes v2", 3500m, DateTime.UtcNow);
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
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Name", "Desc", 100m);

        var result = await _controller.Update(routeId, command, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _mediatorMock.Verify(m => m.Send(It.IsAny<IRequest<ServiceJobDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.Is<DeleteServiceJobCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
