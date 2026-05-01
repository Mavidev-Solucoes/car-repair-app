using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

/// <summary>Opens a new service. The authenticated employee is auto-assigned.</summary>
public record OpenServiceCommand(Guid VehicleId, Guid CustomerId) : IRequest<ServiceOrderDto>;

/// <summary>Adds a service item (with quantity) to a service in Received or Diagnosing status.</summary>
public record AddServiceItemCommand(Guid ServiceOrderId, string Description, decimal Price, int Quantity) : IRequest<ServiceOrderDto>;

/// <summary>Removes a service item from a service in Diagnosing status.</summary>
public record RemoveServiceItemCommand(Guid ServiceOrderId, Guid ServiceItemId) : IRequest<ServiceOrderDto>;

/// <summary>Adds a new job to a service in Received or Diagnosing status.</summary>
public record AddServiceJobCommand(Guid ServiceOrderId, string Name, string Description, int UnitCost) : IRequest<ServiceOrderDto>;

/// <summary>
/// Moves a service from Diagnosing to WaitingForApproval.
/// All jobs must be Acknowledged. Sends a notification email to the customer.
/// </summary>
public record RequestApprovalCommand(Guid ServiceOrderId) : IRequest<ServiceOrderDto>;

/// <summary>
/// Customer approves the service (unauthenticated).
/// Transitions from WaitingForApproval to Executing.
/// </summary>
public record ApproveServiceCommand(Guid ServiceOrderId) : IRequest<ServiceOrderDto>;

/// <summary>Marks the service as Delivered (from Finished status).</summary>
public record DeliverServiceCommand(Guid ServiceOrderId) : IRequest<ServiceOrderDto>;

/// <summary>
/// Customer disputes the finished service. Goes back to Diagnosing so
/// new items and jobs can be added.
/// </summary>
public record DisputeServiceCommand(Guid ServiceOrderId) : IRequest<ServiceOrderDto>;

/// <summary>Gets the status-change history timeline for a service order.</summary>
public record GetServiceStatusHistoryCommand(Guid ServiceOrderId) : IRequest<IEnumerable<ServiceStatusHistoryDto>>;
