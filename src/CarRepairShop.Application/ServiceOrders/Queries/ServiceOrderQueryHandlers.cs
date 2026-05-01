using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Queries;

public class GetServiceOrderByIdQueryHandler : IRequestHandler<GetServiceOrderByIdQuery, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;

    public GetServiceOrderByIdQueryHandler(IServiceOrderRepository serviceOrderRepository)
    {
        _serviceOrderRepository = serviceOrderRepository;
    }

    public async Task<ServiceOrderDto> Handle(GetServiceOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithItemsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        return ServiceOrderQueryMapper.MapToDto(order);
    }
}

public class GetAllServiceOrdersQueryHandler : IRequestHandler<GetAllServiceOrdersQuery, IEnumerable<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;

    public GetAllServiceOrdersQueryHandler(IServiceOrderRepository serviceOrderRepository)
    {
        _serviceOrderRepository = serviceOrderRepository;
    }

    public async Task<IEnumerable<ServiceOrderDto>> Handle(GetAllServiceOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _serviceOrderRepository.GetAllWithDetailsAsync(cancellationToken);
        return orders.Select(ServiceOrderQueryMapper.MapToDto);
    }
}

public class GetServiceOrdersByVehicleIdQueryHandler : IRequestHandler<GetServiceOrdersByVehicleIdQuery, IEnumerable<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;

    public GetServiceOrdersByVehicleIdQueryHandler(IServiceOrderRepository serviceOrderRepository)
    {
        _serviceOrderRepository = serviceOrderRepository;
    }

    public async Task<IEnumerable<ServiceOrderDto>> Handle(GetServiceOrdersByVehicleIdQuery request, CancellationToken cancellationToken)
    {
        var orders = await _serviceOrderRepository.GetByVehicleIdAsync(request.VehicleId, cancellationToken);
        return orders.Select(ServiceOrderQueryMapper.MapToDto);
    }
}

file static class ServiceOrderQueryMapper
{
    public static ServiceOrderDto MapToDto(ServiceOrder order)
    {
        var items = order.ServiceItems.Select(i => new ServiceOrderItemDto(i.Id, i.ServiceOrderId, i.Description, i.Price, i.Quantity));
        return new ServiceOrderDto(order.Id, order.VehicleId, order.Description, order.Status.ToString(), order.TotalPrice, order.CompletedAt, order.Notes, order.CreatedAt, items);
    }
}
