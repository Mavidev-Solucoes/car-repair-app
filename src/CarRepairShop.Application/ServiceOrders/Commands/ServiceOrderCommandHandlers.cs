using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using CarRepairShop.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace CarRepairShop.Application.ServiceOrders.Commands;

// ── Open ─────────────────────────────────────────────────────────────────────

public class OpenServiceCommandHandler : IRequestHandler<OpenServiceCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;

    public OpenServiceCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IVehicleRepository vehicleRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _vehicleRepository = vehicleRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
    }

    public async Task<ServiceOrderDto> Handle(OpenServiceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to open a service.");

        var employee = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        if (employee.UserType != UserType.Employee)
            throw new BusinessException("Only employees can open a service.");

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var serviceOrder = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var body = await _emailTemplateService.RenderServiceReceivedAsync(serviceOrder, customer, vehicle, employee);
        await _emailService.SendAsync(customer.Email, customer.Name,
            "Your service request has been received", body, isHtml: true, cancellationToken);

        return ServiceOrderMapper.MapToDto(serviceOrder);
    }
}

// ── Add Service Item ──────────────────────────────────────────────────────────

public class AddServiceItemCommandHandler : IRequestHandler<AddServiceItemCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddServiceItemCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(AddServiceItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var item = new ServiceOrderItem(order.Id, request.Description, request.Price, request.Quantity);
        order.AddServiceItem(item, userId);

        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

// ── Remove Service Item ───────────────────────────────────────────────────────

public class RemoveServiceItemCommandHandler : IRequestHandler<RemoveServiceItemCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveServiceItemCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(RemoveServiceItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        order.RemoveServiceItem(request.ServiceItemId, userId);

        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

// ── Add Service Job ───────────────────────────────────────────────────────────

public class AddServiceJobCommandHandler : IRequestHandler<AddServiceJobCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddServiceJobCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(AddServiceJobCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var job = new ServiceJob(order.Id, request.Name, request.Description, request.UnitCost, userId);
        order.AttachServiceJob(job, userId);

        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

// ── Request Approval ─────────────────────────────────────────────────────────

public class RequestApprovalCommandHandler : IRequestHandler<RequestApprovalCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly AppSettings _appSettings;

    public RequestApprovalCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AppSettings> appSettings)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _appSettings = appSettings.Value;
    }

    public async Task<ServiceOrderDto> Handle(RequestApprovalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        order.RequestApproval(userId);

        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), order.CustomerId);

        var approvalUrl = $"{_appSettings.BaseUrl.TrimEnd('/')}/api/services/{order.Id}/approve";
        var body = await _emailTemplateService.RenderWaitingForApprovalAsync(order, customer, approvalUrl);
        await _emailService.SendAsync(customer.Email, customer.Name,
            "Your service requires your approval", body, isHtml: true, cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

// ── Approve ───────────────────────────────────────────────────────────────────

public class ApproveServiceCommandHandler : IRequestHandler<ApproveServiceCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveServiceCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderDto> Handle(ApproveServiceCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        order.Approve();

        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

// ── Deliver ───────────────────────────────────────────────────────────────────

public class DeliverServiceCommandHandler : IRequestHandler<DeliverServiceCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeliverServiceCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(DeliverServiceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        order.Deliver(userId);

        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

// ── Dispute ───────────────────────────────────────────────────────────────────

public class DisputeServiceCommandHandler : IRequestHandler<DisputeServiceCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DisputeServiceCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(DisputeServiceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        order.Dispute(userId);

        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

// ── Get Status History ────────────────────────────────────────────────────────

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

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class ServiceOrderMapper
{
    public static ServiceOrderDto MapToDto(ServiceOrder order)
    {
        var items = order.ServiceItems
            .Select(i => new ServiceOrderItemDto(i.Id, i.ServiceOrderId, i.Description, i.Price, i.Quantity));

        var jobs = order.ServiceJobs
            .Select(j => new ServiceJobDto(
                j.Id, j.ServiceOrderId, j.Name, j.Description, j.UnitCost,
                j.Status.ToString(), j.AssignedUserId, j.CreatedAt, j.CreatedUserId, j.LastUpdatedUserId));

        var history = order.StatusHistory
            .Select(h => new ServiceStatusHistoryDto(
                h.Id, h.ServiceOrderId, h.FromStatus?.ToString(), h.ToStatus.ToString(),
                h.ChangedAt, h.ChangedByUserId));

        return new ServiceOrderDto(
            order.Id,
            order.VehicleId,
            order.CustomerId,
            order.AssignedUserId,
            order.Status.ToString(),
            order.TotalPrice,
            order.CreatedAt,
            items,
            jobs,
            history);
    }
}
