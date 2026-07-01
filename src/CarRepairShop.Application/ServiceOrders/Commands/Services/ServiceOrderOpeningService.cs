using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public class ServiceOrderOpeningService : IServiceOrderOpeningService
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceOrderNotificationService _serviceOrderNotificationService;

    public ServiceOrderOpeningService(
        IServiceOrderRepository serviceOrderRepository,
        IVehicleRepository vehicleRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IServiceOrderNotificationService serviceOrderNotificationService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _vehicleRepository = vehicleRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _serviceOrderNotificationService = serviceOrderNotificationService;
    }

    public async Task<ServiceOrderDto> OpenAsync(OpenServiceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to open a service.");

        var employee = await _userRepository.GetEmployeeByIdAsync(userId, cancellationToken)
            ?? throw new BusinessException("Only employees can open a service.");

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var serviceOrder = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _serviceOrderNotificationService.NotifyServiceReceivedAsync(serviceOrder, customer, vehicle, employee, cancellationToken);

        return ServiceOrderMapper.MapToDto(serviceOrder);
    }
}
