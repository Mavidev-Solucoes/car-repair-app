using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Queries;

public class GetServiceOrderByIdQueryHandler : IRequestHandler<GetServiceOrderByIdQuery, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetServiceOrderByIdQueryHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(GetServiceOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        var currentUserId = _currentUserService.UserId;
        if (currentUserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (user?.Role == Domain.Enums.UserRole.Customer && order.CustomerId != currentUserId.Value)
                throw new BusinessException("Customers can only access their own services.");
        }

        return ServiceOrderMapper.MapToDto(order);
    }
}

public class GetAllServiceOrdersQueryHandler : IRequestHandler<GetAllServiceOrdersQuery, IEnumerable<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllServiceOrdersQueryHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<ServiceOrderDto>> Handle(GetAllServiceOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _serviceOrderRepository.GetAllWithDetailsAsync(cancellationToken);
        var filteredOrders = orders;

        var currentUserId = _currentUserService.UserId;
        if (currentUserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (user?.Role == Domain.Enums.UserRole.Customer)
                filteredOrders = filteredOrders.Where(order => order.CustomerId == currentUserId.Value);
        }

        // Finished (5) and Delivered (6) are always excluded (soft-delete behaviour).
        filteredOrders = filteredOrders.Where(o =>
            o.Status != Domain.Enums.ServiceStatus.Finished &&
            o.Status != Domain.Enums.ServiceStatus.Delivered);

        // Business-priority ordering: most actionable statuses first.
        // Executing(4) > WaitingForApproval(3) > Diagnosing(2) > Received(1)
        // Tie-break: oldest creation date first.
        filteredOrders = filteredOrders
            .OrderBy(o => o.Status switch
            {
                Domain.Enums.ServiceStatus.Executing          => 1,
                Domain.Enums.ServiceStatus.WaitingForApproval => 2,
                Domain.Enums.ServiceStatus.Diagnosing         => 3,
                Domain.Enums.ServiceStatus.Received           => 4,
                _                                             => 5
            })
            .ThenBy(o => o.CreatedAt);

        return filteredOrders.Select(ServiceOrderMapper.MapToDto);
    }
}
