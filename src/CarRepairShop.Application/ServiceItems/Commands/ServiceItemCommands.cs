using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceItems.Commands;

public record CreateServiceItemCommand(string Name, string Description, int UnitCost) : IRequest<ServiceItemDto>;

public record UpdateServiceItemCommand(Guid Id, string Name, string Description, int UnitCost) : IRequest<ServiceItemDto>;

public record DeleteServiceItemCommand(Guid Id) : IRequest<Unit>;
