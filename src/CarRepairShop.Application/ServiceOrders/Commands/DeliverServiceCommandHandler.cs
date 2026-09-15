using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class DeliverServiceCommandHandler : IRequestHandler<DeliverServiceCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceOrderHistoryTracker _historyTracker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceOrderBusinessTelemetry _serviceOrderBusinessTelemetry;

    public DeliverServiceCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IServiceOrderHistoryTracker historyTracker,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IServiceOrderBusinessTelemetry serviceOrderBusinessTelemetry)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _historyTracker = historyTracker;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _serviceOrderBusinessTelemetry = serviceOrderBusinessTelemetry;
    }

    public async Task<ServiceOrderDto> Handle(DeliverServiceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var statusHistoryCount = order.StatusHistory.Count;
        order.Deliver(userId);
        await _historyTracker.AddLatestAsync(order, statusHistoryCount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        await _serviceOrderBusinessTelemetry.RecordStatusChangesAsync(order, statusHistoryCount, cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}
