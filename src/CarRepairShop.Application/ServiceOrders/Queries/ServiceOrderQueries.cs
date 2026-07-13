using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Queries;

public record GetServiceOrderByIdQuery(Guid Id) : IRequest<ServiceOrderDto>;

/// <summary>
/// Returns all active service orders (Finished and Delivered are always excluded).
/// Results are sorted by operational priority: Executing → WaitingForApproval → Diagnosing → Received,
/// then by creation date ascending (oldest first).
/// </summary>
public record GetAllServiceOrdersQuery() : IRequest<IEnumerable<ServiceOrderDto>>;
