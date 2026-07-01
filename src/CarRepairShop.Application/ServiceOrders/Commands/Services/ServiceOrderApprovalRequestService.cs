using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public class ServiceOrderApprovalRequestService : IServiceOrderApprovalRequestService
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IServiceOrderHistoryTracker _historyTracker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceOrderNotificationService _serviceOrderNotificationService;

    public ServiceOrderApprovalRequestService(
        IServiceOrderRepository serviceOrderRepository,
        ICustomerRepository customerRepository,
        IServiceOrderHistoryTracker historyTracker,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IServiceOrderNotificationService serviceOrderNotificationService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _customerRepository = customerRepository;
        _historyTracker = historyTracker;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _serviceOrderNotificationService = serviceOrderNotificationService;
    }

    public async Task<ServiceOrderDto> RequestApprovalAsync(RequestApprovalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var statusHistoryCount = order.StatusHistory.Count;
        order.RequestApproval(userId);
        await _historyTracker.AddLatestAsync(order, statusHistoryCount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), order.CustomerId);

        await _serviceOrderNotificationService.NotifyApprovalRequestedAsync(order, customer, cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}
