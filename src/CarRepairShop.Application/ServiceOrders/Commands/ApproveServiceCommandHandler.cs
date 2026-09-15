using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class ApproveServiceCommandHandler : IRequestHandler<ApproveServiceCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceOrderHistoryTracker _historyTracker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceOrderBusinessTelemetry _serviceOrderBusinessTelemetry;

    public ApproveServiceCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IServiceOrderHistoryTracker historyTracker,
        IUnitOfWork unitOfWork,
        IServiceOrderBusinessTelemetry serviceOrderBusinessTelemetry)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _historyTracker = historyTracker;
        _unitOfWork = unitOfWork;
        _serviceOrderBusinessTelemetry = serviceOrderBusinessTelemetry;
    }

    public async Task<ServiceOrderDto> Handle(ApproveServiceCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var statusHistoryCount = order.StatusHistory.Count;
        order.Approve();
        await _historyTracker.AddLatestAsync(order, statusHistoryCount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        await _serviceOrderBusinessTelemetry.RecordStatusChangesAsync(order, statusHistoryCount, cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}
