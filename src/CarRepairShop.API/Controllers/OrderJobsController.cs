using CarRepairShop.Application.OrderJobs.Commands;
using CarRepairShop.Application.OrderJobs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRepairShop.API.Controllers;

[ApiController]
[Route("api/order-jobs")]
[Authorize]
public class OrderJobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderJobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetOrderJobsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderJobByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderJobHistoryQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcknowledgeOrderJobCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/start-progress")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> StartProgress(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartOrderJobProgressCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteOrderJobCommand(id), cancellationToken);
        return Ok(result);
    }
}
