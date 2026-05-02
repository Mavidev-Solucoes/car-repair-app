using System.Linq.Expressions;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.ServiceItems.Queries;

public class GetServiceItemByIdQueryHandler : IRequestHandler<GetServiceItemByIdQuery, ServiceItemDto>
{
    private readonly IServiceItemRepository _serviceItemRepository;

    public GetServiceItemByIdQueryHandler(IServiceItemRepository serviceItemRepository)
    {
        _serviceItemRepository = serviceItemRepository;
    }

    public async Task<ServiceItemDto> Handle(GetServiceItemByIdQuery request, CancellationToken cancellationToken)
    {
        var serviceItem = await _serviceItemRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceItem), request.Id);

        return ServiceItemQueryMapper.MapToDto(serviceItem);
    }
}

public class GetServiceItemsQueryHandler : IRequestHandler<GetServiceItemsQuery, PagedResult<ServiceItemDto>>
{
    private readonly IServiceItemRepository _serviceItemRepository;

    public GetServiceItemsQueryHandler(IServiceItemRepository serviceItemRepository)
    {
        _serviceItemRepository = serviceItemRepository;
    }

    public async Task<PagedResult<ServiceItemDto>> Handle(GetServiceItemsQuery request, CancellationToken cancellationToken)
    {
        var filters = BuildFilters(request);

        var (items, totalCount) = await _serviceItemRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        return new PagedResult<ServiceItemDto>(
            items.Select(ServiceItemQueryMapper.MapToDto),
            totalCount,
            request.Page,
            request.PageSize);
    }

    private static IEnumerable<Expression<Func<ServiceItem, bool>>> BuildFilters(GetServiceItemsQuery request)
    {
        var filters = new List<Expression<Func<ServiceItem, bool>>>();

        if (!string.IsNullOrWhiteSpace(request.Name))
            filters.Add(si => si.Name.Contains(request.Name));

        return filters;
    }
}

file static class ServiceItemQueryMapper
{
    internal static ServiceItemDto MapToDto(ServiceItem serviceItem) =>
        new(serviceItem.Id, serviceItem.Name, serviceItem.Description, serviceItem.Price, serviceItem.Stock,
            serviceItem.CreatedAt, serviceItem.CreatedUserId, serviceItem.LastUpdatedUserId);
}
