using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Queries;

public record GetServiceOrderByIdQuery(Guid Id) : IRequest<ServiceOrderDto>;

/// <summary>
/// Returns all visible service orders.
/// <para>
/// When <paramref name="IncludeCompleted"/> is <c>false</c>, orders in
/// <c>Finished</c> or <c>Delivered</c> status are excluded from the results.
/// </para>
/// Results are sorted by operational priority:
/// WaitingForApproval → Executing → Diagnosing → Received → Finished → Delivered.
/// </summary>
public record GetAllServiceOrdersQuery(bool IncludeCompleted = true) : IRequest<IEnumerable<ServiceOrderDto>>;
