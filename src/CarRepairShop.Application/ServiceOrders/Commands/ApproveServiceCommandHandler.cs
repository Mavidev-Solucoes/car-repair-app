using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class ApproveServiceCommandHandler : IRequestHandler<ApproveServiceCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceStatusHistoryRepository _serviceStatusHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveServiceCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IServiceStatusHistoryRepository serviceStatusHistoryRepository,
        IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _serviceStatusHistoryRepository = serviceStatusHistoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderDto> Handle(ApproveServiceCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var statusHistoryCount = order.StatusHistory.Count;
        order.Approve();
        await ServiceOrderHistoryPersistence.AddLatestAsync(
            order,
            statusHistoryCount,
            _serviceStatusHistoryRepository,
            cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}
