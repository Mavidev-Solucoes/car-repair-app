using CarRepairShop.Application.ServiceJobs.Commands;
using CarRepairShop.Application.ServiceJobs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRepairShop.API.Controllers;

/// <summary>
/// Manages status transitions for service jobs.
/// Jobs are created via POST /api/services/{id}/jobs.
/// </summary>
[ApiController]
[Route("api/service-jobs")]
[Authorize]
public class ServiceJobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServiceJobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get service jobs with pagination, ordering and filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetServiceJobsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a service job by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceJobByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get status change history and timeline for a service job.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceJobHistoryQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update a service job (only allowed while Open and service is Diagnosing).
    /// Only the assigned service employee can do this.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceJobCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route ID and body ID do not match.");

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Acknowledge a job. The authenticated employee takes responsibility for this job.
    /// Only allowed while the service is in Diagnosing status.
    /// </summary>
    [HttpPatch("{id:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcknowledgeJobCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Start progress on a job. Only the assigned employee can do this.
    /// Only allowed after the customer has approved (service in Executing status).
    /// </summary>
    [HttpPatch("{id:guid}/start-progress")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> StartProgress(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartJobProgressCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Complete a job. Only the assigned employee can do this.
    /// When all jobs in the service are completed, the service automatically transitions to Finished
    /// and a notification email is sent to the customer.
    /// </summary>
    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteJobCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a service job (only while Open and service is Diagnosing). Requires Admin role.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteServiceJobCommand(id), cancellationToken);
        return NoContent();
    }
}
