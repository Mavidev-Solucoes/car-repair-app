using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands;
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
        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        return ServiceOrderMapper.MapToDto(order);
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
        return orders.Select(ServiceOrderMapper.MapToDto);
    }
}
