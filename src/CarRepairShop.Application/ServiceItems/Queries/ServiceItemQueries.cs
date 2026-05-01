using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceItems.Queries;

public record GetServiceItemByIdQuery(Guid Id) : IRequest<ServiceItemDto>;

public record GetServiceItemsQuery : IRequest<PagedResult<ServiceItemDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; } = false;

    // Filters
    public string? Name { get; init; }
}
