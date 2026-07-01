using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class AddServiceItemCommandHandler : IRequestHandler<AddServiceItemCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceOrderItemRepository _serviceOrderItemRepository;
    private readonly IServiceItemRepository _serviceItemRepository;
    private readonly IServiceOrderHistoryTracker _historyTracker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddServiceItemCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IServiceOrderItemRepository serviceOrderItemRepository,
        IServiceItemRepository serviceItemRepository,
        IServiceOrderHistoryTracker historyTracker,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _serviceOrderItemRepository = serviceOrderItemRepository;
        _serviceItemRepository = serviceItemRepository;
        _historyTracker = historyTracker;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(AddServiceItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var itemCatalog = await _serviceItemRepository.GetByIdAsync(request.ServiceItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceItem), request.ServiceItemId);

        var statusHistoryCount = order.StatusHistory.Count;
        var item = new ServiceOrderItem(order.Id, itemCatalog.Id, itemCatalog.Description, itemCatalog.Price, request.Quantity);
        order.AddServiceItem(item, userId);
        await _serviceOrderItemRepository.AddAsync(item, cancellationToken);
        await _historyTracker.AddLatestAsync(order, statusHistoryCount, cancellationToken);
        itemCatalog.ReserveStock(request.Quantity, userId);

        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}
