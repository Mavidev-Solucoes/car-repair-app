using CarRepairShop.Application.ServiceOrders.Commands;
using CarRepairShop.Application.ServiceOrders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRepairShop.API.Controllers;

/// <summary>
/// Manages the full lifecycle of a vehicle service order.
/// Each endpoint performs exactly one business action.
/// </summary>
[ApiController]
[Route("api/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Get all services with full details.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllServiceOrdersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a service by ID with all details (items, jobs, status history).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceOrderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get the status-change timeline for a service.</summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceStatusHistoryCommand(id), cancellationToken);
        return Ok(result);
    }

    // ── Status 1: Open (Received) ──────────────────────────────────────────

    /// <summary>
    /// Opens a new service. The authenticated employee is auto-assigned.
    /// Requires a vehicle and a customer. Sends a confirmation email.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Open([FromBody] OpenServiceCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // ── Status 1→2: Add items/jobs (Diagnosing) ────────────────────────────

    /// <summary>
    /// Adds a service item (with quantity) to a service.
    /// Only the assigned employee can do this. Auto-transitions to Diagnosing on first insertion.
    /// </summary>
    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddServiceItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddServiceItemCommand(id, request.Description, request.Price, request.Quantity),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Removes a service item. Only allowed while Diagnosing.</summary>
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveServiceItemCommand(id, itemId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Adds a new job to a service. Only the assigned employee can do this.
    /// Auto-transitions to Diagnosing on first insertion.
    /// </summary>
    [HttpPost("{id:guid}/jobs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddJob(Guid id, [FromBody] AddServiceJobRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddServiceJobCommand(id, request.Name, request.Description, request.UnitCost),
            cancellationToken);
        return Ok(result);
    }

    // ── Status 2→3: Request Approval (WaitingForApproval) ─────────────────

    /// <summary>
    /// Moves the service to WaitingForApproval. All jobs must be Acknowledged.
    /// Only the assigned employee can trigger this. Sends an approval email to the customer.
    /// </summary>
    [HttpPatch("{id:guid}/request-approval")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestApproval(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RequestApprovalCommand(id), cancellationToken);
        return Ok(result);
    }

    // ── Status 3→4: Customer Approves (Executing) ─────────────────────────

    /// <summary>
    /// Customer approval endpoint (no authentication required).
    /// The customer clicks this link from the email to approve the service.
    /// Transitions from WaitingForApproval to Executing.
    /// </summary>
    [HttpGet("{id:guid}/approve")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ApproveServiceCommand(id), cancellationToken);
        return Ok(result);
    }

    // ── Status 5→6: Deliver ────────────────────────────────────────────────

    /// <summary>
    /// Marks a finished service as Delivered after the customer collects the vehicle.
    /// </summary>
    [HttpPatch("{id:guid}/deliver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeliverServiceCommand(id), cancellationToken);
        return Ok(result);
    }

    // ── Status 5→2: Dispute ────────────────────────────────────────────────

    /// <summary>
    /// Customer disputes a finished service. Goes back to Diagnosing so new items
    /// and jobs can be added, and the process flows through again.
    /// </summary>
    [HttpPatch("{id:guid}/dispute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Dispute(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DisputeServiceCommand(id), cancellationToken);
        return Ok(result);
    }
}

/// <summary>Request body for adding a service item.</summary>
public record AddServiceItemRequest(string Description, decimal Price, int Quantity);

/// <summary>Request body for adding a service job.</summary>
public record AddServiceJobRequest(string Name, string Description, int UnitCost);
