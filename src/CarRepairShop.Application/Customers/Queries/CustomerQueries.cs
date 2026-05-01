using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.Customers.Queries;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public record GetCustomerWithVehiclesQuery(Guid Id) : IRequest<CustomerDto>;

public record GetCustomersQuery : IRequest<PagedResult<CustomerDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; } = false;

    // Filters
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? PersonalId { get; init; }
    public string? Telephone { get; init; }
}
