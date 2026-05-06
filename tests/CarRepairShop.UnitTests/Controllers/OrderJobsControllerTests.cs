using CarRepairShop.API.Controllers;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.OrderJobs.Commands;
using CarRepairShop.Application.OrderJobs.Queries;
using CarRepairShop.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class OrderJobsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly OrderJobsController _controller;

    public OrderJobsControllerTests()
    {
        _controller = new OrderJobsController(_mediatorMock.Object);
    }

    private static ServiceOrderJobDto SampleJobDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m,
            "Open", null, DateTime.UtcNow);

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var expected = new PagedResult<ServiceOrderJobDto>([], 0, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOrderJobsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetOrderJobsQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleJobDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<GetOrderJobByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetHistory_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var history = new List<ServiceOrderJobStatusHistoryDto>();
        _mediatorMock.Setup(m => m.Send(It.Is<GetOrderJobHistoryQuery>(q => q.OrderJobId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = await _controller.GetHistory(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(history, ok.Value);
    }

    [Fact]
    public async Task Acknowledge_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleJobDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<AcknowledgeOrderJobCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Acknowledge(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task StartProgress_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleJobDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<StartOrderJobProgressCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.StartProgress(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Complete_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleJobDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<CompleteOrderJobCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Complete(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }
}
