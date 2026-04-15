using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.Customers.Queries;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public record GetAllCustomersQuery : IRequest<IEnumerable<CustomerDto>>;

public record GetCustomerWithVehiclesQuery(Guid Id) : IRequest<CustomerDto>;
