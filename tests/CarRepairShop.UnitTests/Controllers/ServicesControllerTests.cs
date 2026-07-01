using CarRepairShop.API.Controllers;
using CarRepairShop.API.Requests;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands;
using CarRepairShop.Application.ServiceOrders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class ServicesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ServicesController _controller;

    public ServicesControllerTests()
    {
        _controller = new ServicesController(_mediatorMock.Object);
    }

    private static ServiceOrderDto SampleOrderDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Received", 0m, DateTime.UtcNow, [], [], []);

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var expected = new List<ServiceOrderDto> { SampleOrderDto() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllServiceOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<GetServiceOrderByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetHistory_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var history = new List<ServiceStatusHistoryDto>();
        _mediatorMock.Setup(m => m.Send(It.Is<GetServiceStatusHistoryCommand>(c => c.ServiceOrderId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = await _controller.GetHistory(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(history, ok.Value);
    }

    [Fact]
    public async Task Open_ReturnsCreatedAtAction()
    {
        var command = new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid());
        var dto = SampleOrderDto();
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Open(command, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetById), created.ActionName);
        Assert.Equal(dto, created.Value);
    }

    [Fact]
    public async Task AddItem_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var request = new AddServiceItemRequest(Guid.NewGuid(), 2);
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<AddServiceItemCommand>(c => c.ServiceOrderId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.AddItem(id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task RemoveItem_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<RemoveServiceItemCommand>(c => c.ServiceOrderId == id && c.ServiceItemId == itemId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.RemoveItem(id, itemId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task AddJob_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var request = new AddServiceJobRequest(Guid.NewGuid());
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<AddServiceJobCommand>(c => c.ServiceOrderId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.AddJob(id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task RemoveJob_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<RemoveServiceJobCommand>(c => c.ServiceOrderId == id && c.ServiceJobId == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.RemoveJob(id, jobId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task RequestApproval_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<RequestApprovalCommand>(c => c.ServiceOrderId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.RequestApproval(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Approve_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<ApproveServiceCommand>(c => c.ServiceOrderId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Approve(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Deliver_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<DeliverServiceCommand>(c => c.ServiceOrderId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Deliver(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Dispute_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleOrderDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<DisputeServiceCommand>(c => c.ServiceOrderId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Dispute(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }
}
