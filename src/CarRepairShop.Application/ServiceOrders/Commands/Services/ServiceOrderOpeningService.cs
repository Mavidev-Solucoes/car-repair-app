using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public class ServiceOrderOpeningService : IServiceOrderOpeningService
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceOrderItemRepository _serviceOrderItemRepository;
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;
    private readonly IServiceItemRepository _serviceItemRepository;
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceOrderNotificationService _serviceOrderNotificationService;
    private readonly IServiceOrderBusinessTelemetry _serviceOrderBusinessTelemetry;

    public ServiceOrderOpeningService(
        IServiceOrderRepository serviceOrderRepository,
        IServiceOrderItemRepository serviceOrderItemRepository,
        IServiceOrderJobRepository serviceOrderJobRepository,
        IServiceItemRepository serviceItemRepository,
        IServiceJobRepository serviceJobRepository,
        IVehicleRepository vehicleRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IServiceOrderNotificationService serviceOrderNotificationService,
        IServiceOrderBusinessTelemetry serviceOrderBusinessTelemetry)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _serviceOrderItemRepository = serviceOrderItemRepository;
        _serviceOrderJobRepository = serviceOrderJobRepository;
        _serviceItemRepository = serviceItemRepository;
        _serviceJobRepository = serviceJobRepository;
        _vehicleRepository = vehicleRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _serviceOrderNotificationService = serviceOrderNotificationService;
        _serviceOrderBusinessTelemetry = serviceOrderBusinessTelemetry;
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
        var statusHistoryCount = serviceOrder.StatusHistory.Count;
        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);

        if (request.Items is not null)
        {
            foreach (var itemInput in request.Items)
            {
                var catalogItem = await _serviceItemRepository.GetByIdAsync(itemInput.ServiceItemId, cancellationToken)
                    ?? throw new NotFoundException(nameof(ServiceItem), itemInput.ServiceItemId);

                var orderItem = new ServiceOrderItem(serviceOrder.Id, catalogItem.Id, catalogItem.Description, catalogItem.Price, itemInput.Quantity);
                serviceOrder.AddServiceItem(orderItem, employee.Id);
                await _serviceOrderItemRepository.AddAsync(orderItem, cancellationToken);
                catalogItem.ReserveStock(itemInput.Quantity, employee.Id);
            }
        }

        if (request.Jobs is not null)
        {
            foreach (var jobInput in request.Jobs)
            {
                var catalogJob = await _serviceJobRepository.GetByIdAsync(jobInput.ServiceJobId, cancellationToken)
                    ?? throw new NotFoundException(nameof(ServiceJob), jobInput.ServiceJobId);

                var orderJob = new ServiceOrderJob(serviceOrder.Id, catalogJob.Id, catalogJob.Name, catalogJob.Description, catalogJob.Price, employee.Id);
                serviceOrder.AttachServiceJob(orderJob, employee.Id);
                await _serviceOrderJobRepository.AddAsync(orderJob, cancellationToken);
            }
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        await _serviceOrderBusinessTelemetry.RecordCreatedAsync(serviceOrder, cancellationToken);
        await _serviceOrderBusinessTelemetry.RecordStatusChangesAsync(serviceOrder, statusHistoryCount, cancellationToken);

        await _serviceOrderNotificationService.NotifyServiceReceivedAsync(serviceOrder, customer, vehicle, employee, cancellationToken);

        return ServiceOrderMapper.MapToDto(serviceOrder);
    }
}
