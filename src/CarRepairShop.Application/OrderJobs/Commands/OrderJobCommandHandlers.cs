using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CarRepairShop.Application.OrderJobs.Commands;

public class AcknowledgeOrderJobCommandHandler : IRequestHandler<AcknowledgeOrderJobCommand, ServiceOrderJobDto>
{
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IOrderJobHistoryTracker _jobHistoryTracker;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AcknowledgeOrderJobCommandHandler(
        IServiceOrderJobRepository serviceOrderJobRepository,
        IServiceOrderRepository serviceOrderRepository,
        IOrderJobHistoryTracker jobHistoryTracker,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderJobRepository = serviceOrderJobRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _jobHistoryTracker = jobHistoryTracker;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderJobDto> Handle(AcknowledgeOrderJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceOrderJobRepository.GetByIdWithHistoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrderJob), request.Id);

        var order = await _serviceOrderRepository.GetByIdAsync(serviceJob.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), serviceJob.ServiceOrderId);

        if (order.Status != ServiceStatus.Diagnosing)
            throw new BusinessException("Jobs can only be acknowledged while the service is in Diagnosing status.");

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to acknowledge a job.");

        var user = await _userRepository.GetEmployeeByIdAsync(userId, cancellationToken)
            ?? throw new BusinessException("Only mechanics can acknowledge jobs.");

        if (user.Role is not (UserRole.Mechanic or UserRole.Admin))
            throw new BusinessException("Only mechanics can acknowledge jobs.");

        var statusHistoryCount = serviceJob.StatusHistory.Count;
        serviceJob.Acknowledge(user);
        await _jobHistoryTracker.AddLatestAsync(serviceJob, statusHistoryCount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return OrderJobMapper.MapToDto(serviceJob);
    }
}

public class StartOrderJobProgressCommandHandler : IRequestHandler<StartOrderJobProgressCommand, ServiceOrderJobDto>
{
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IOrderJobHistoryTracker _jobHistoryTracker;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public StartOrderJobProgressCommandHandler(
        IServiceOrderJobRepository serviceOrderJobRepository,
        IServiceOrderRepository serviceOrderRepository,
        IOrderJobHistoryTracker jobHistoryTracker,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderJobRepository = serviceOrderJobRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _jobHistoryTracker = jobHistoryTracker;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderJobDto> Handle(StartOrderJobProgressCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceOrderJobRepository.GetByIdWithHistoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrderJob), request.Id);

        var order = await _serviceOrderRepository.GetByIdAsync(serviceJob.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), serviceJob.ServiceOrderId);

        if (order.Status != ServiceStatus.Executing)
            throw new BusinessException("Jobs can only be started after the customer has approved the service.");

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to start progress on a job.");
        var user = await _userRepository.GetEmployeeByIdAsync(userId, cancellationToken)
            ?? throw new BusinessException("Only mechanics can start job progress.");
        if (user.Role is not (UserRole.Mechanic or UserRole.Admin))
            throw new BusinessException("Only mechanics can start job progress.");

        var statusHistoryCount = serviceJob.StatusHistory.Count;
        serviceJob.StartProgress(userId);
        await _jobHistoryTracker.AddLatestAsync(serviceJob, statusHistoryCount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return OrderJobMapper.MapToDto(serviceJob);
    }
}

public class CompleteOrderJobCommandHandler : IRequestHandler<CompleteOrderJobCommand, ServiceOrderJobDto>
{
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IOrderJobHistoryTracker _jobHistoryTracker;
    private readonly IServiceOrderHistoryTracker _orderHistoryTracker;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<CompleteOrderJobCommandHandler> _logger;

    public CompleteOrderJobCommandHandler(
        IServiceOrderJobRepository serviceOrderJobRepository,
        IServiceOrderRepository serviceOrderRepository,
        IOrderJobHistoryTracker jobHistoryTracker,
        IServiceOrderHistoryTracker orderHistoryTracker,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<CompleteOrderJobCommandHandler> logger)
    {
        _serviceOrderJobRepository = serviceOrderJobRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _jobHistoryTracker = jobHistoryTracker;
        _orderHistoryTracker = orderHistoryTracker;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    public async Task<ServiceOrderJobDto> Handle(CompleteOrderJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceOrderJobRepository.GetByIdWithServiceOrderAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrderJob), request.Id);

        var order = serviceJob.ServiceOrder;
        if (order.Status != ServiceStatus.Executing)
            throw new BusinessException("Jobs can only be completed after the customer has approved the service.");

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to complete a job.");
        var user = await _userRepository.GetEmployeeByIdAsync(userId, cancellationToken)
            ?? throw new BusinessException("Only mechanics can complete jobs.");
        if (user.Role is not (UserRole.Mechanic or UserRole.Admin))
            throw new BusinessException("Only mechanics can complete jobs.");

        var jobStatusHistoryCount = serviceJob.StatusHistory.Count;
        var orderStatusHistoryCount = order.StatusHistory.Count;
        serviceJob.Complete(userId);
        await _jobHistoryTracker.AddLatestAsync(serviceJob, jobStatusHistoryCount, cancellationToken);

        var finished = order.TryFinish();
        await _orderHistoryTracker.AddLatestAsync(order, orderStatusHistoryCount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        if (finished)
        {
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);
            if (customer is not null)
            {
                try
                {
                    var body = await _emailTemplateService.RenderServiceFinishedAsync(order, customer);
                    await _emailService.SendAsync(customer.Email, customer.Name,
                        "Your service has been completed", body, isHtml: true, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send finish notification email for service order {ServiceOrderId}.", order.Id);
                }
            }
        }

        return OrderJobMapper.MapToDto(serviceJob);
    }
}

internal static class OrderJobMapper
{
    internal static ServiceOrderJobDto MapToDto(ServiceOrderJob serviceJob) =>
        new(serviceJob.Id, serviceJob.ServiceOrderId, serviceJob.ServiceJobId, serviceJob.Name, serviceJob.Description,
            serviceJob.Price, serviceJob.Status.ToString(), serviceJob.AssignedUserId, serviceJob.CreatedAt,
            serviceJob.CreatedUserId, serviceJob.LastUpdatedUserId);
}
