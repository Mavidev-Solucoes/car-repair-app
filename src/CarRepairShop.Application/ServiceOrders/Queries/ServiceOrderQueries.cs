using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Queries;

public record GetServiceOrderByIdQuery(Guid Id) : IRequest<ServiceOrderDto>;

public record GetAllServiceOrdersQuery : IRequest<IEnumerable<ServiceOrderDto>>;
