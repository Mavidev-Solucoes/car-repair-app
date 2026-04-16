using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public record CreateServiceOrderCommand(Guid VehicleId, string Description, string? Notes) : IRequest<ServiceOrderDto>;

public record UpdateServiceOrderCommand(Guid Id, string Description, string? Notes) : IRequest<ServiceOrderDto>;

public record UpdateServiceOrderStatusCommand(Guid Id, ServiceOrderStatus Status) : IRequest<ServiceOrderDto>;

public record AddServiceItemCommand(Guid ServiceOrderId, string Description, decimal Price, int Quantity) : IRequest<ServiceOrderDto>;

public record RemoveServiceItemCommand(Guid ServiceOrderId, Guid ServiceItemId) : IRequest<ServiceOrderDto>;

public record DeleteServiceOrderCommand(Guid Id) : IRequest<Unit>;
