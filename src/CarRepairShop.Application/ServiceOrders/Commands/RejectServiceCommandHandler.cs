using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class RejectServiceCommandHandler : IRequestHandler<RejectServiceCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceOrderHistoryTracker _historyTracker;
    private readonly IUnitOfWork _unitOfWork;

    public RejectServiceCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IServiceOrderHistoryTracker historyTracker,
        IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _historyTracker = historyTracker;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderDto> Handle(RejectServiceCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var statusHistoryCount = order.StatusHistory.Count;
        order.Reject();
        await _historyTracker.AddLatestAsync(order, statusHistoryCount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}
