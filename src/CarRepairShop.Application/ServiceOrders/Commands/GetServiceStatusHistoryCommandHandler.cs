using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class GetServiceStatusHistoryCommandHandler : IRequestHandler<GetServiceStatusHistoryCommand, IEnumerable<ServiceStatusHistoryDto>>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;

    public GetServiceStatusHistoryCommandHandler(IServiceOrderRepository serviceOrderRepository)
    {
        _serviceOrderRepository = serviceOrderRepository;
    }

    public async Task<IEnumerable<ServiceStatusHistoryDto>> Handle(GetServiceStatusHistoryCommand request, CancellationToken cancellationToken)
    {
        _ = await _serviceOrderRepository.GetByIdAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var history = await _serviceOrderRepository.GetStatusHistoryAsync(request.ServiceOrderId, cancellationToken);
        return history.Select(h => new ServiceStatusHistoryDto(
            h.Id,
            h.ServiceOrderId,
            h.FromStatus?.ToString(),
            h.ToStatus.ToString(),
            h.ChangedAt,
            h.ChangedByUserId));
    }
}
