using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceJobs.Commands;

public class UpdateServiceJobCommandHandler : IRequestHandler<UpdateServiceJobCommand, ServiceJobDto>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateServiceJobCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceJobDto> Handle(UpdateServiceJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        if (serviceJob.Status != JobStatus.Open)
            throw new BusinessException("Only Open jobs can be updated.");

        var order = await _serviceOrderRepository.GetByIdAsync(serviceJob.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), serviceJob.ServiceOrderId);

        if (order.Status != ServiceStatus.Diagnosing)
            throw new BusinessException("Jobs can only be updated while the service is in Diagnosing status.");

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        if (order.AssignedUserId != userId)
            throw new BusinessException("Only the assigned employee can update jobs on this service.");

        serviceJob.Update(request.Name, request.Description, request.UnitCost, userId);
        _serviceJobRepository.Update(serviceJob);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceJobMapper.MapToDto(serviceJob);
    }
}

public class DeleteServiceJobCommandHandler : IRequestHandler<DeleteServiceJobCommand, Unit>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteServiceJobCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteServiceJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        if (serviceJob.Status != JobStatus.Open)
            throw new BusinessException("Only Open jobs can be deleted.");

        var order = await _serviceOrderRepository.GetByIdAsync(serviceJob.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), serviceJob.ServiceOrderId);

        if (order.Status != ServiceStatus.Diagnosing)
            throw new BusinessException("Jobs can only be deleted while the service is in Diagnosing status.");

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        if (order.AssignedUserId != userId)
            throw new BusinessException("Only the assigned employee can delete jobs on this service.");

        _serviceJobRepository.Delete(serviceJob);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}

public class AcknowledgeJobCommandHandler : IRequestHandler<AcknowledgeJobCommand, ServiceJobDto>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AcknowledgeJobCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IServiceOrderRepository serviceOrderRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceJobDto> Handle(AcknowledgeJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdWithHistoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        var order = await _serviceOrderRepository.GetByIdAsync(serviceJob.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), serviceJob.ServiceOrderId);

        if (order.Status != ServiceStatus.Diagnosing)
            throw new BusinessException("Jobs can only be acknowledged while the service is in Diagnosing status.");

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to acknowledge a job.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        serviceJob.Acknowledge(user);
        _serviceJobRepository.Update(serviceJob);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceJobMapper.MapToDto(serviceJob);
    }
}

public class StartJobProgressCommandHandler : IRequestHandler<StartJobProgressCommand, ServiceJobDto>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public StartJobProgressCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceJobDto> Handle(StartJobProgressCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdWithHistoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        var order = await _serviceOrderRepository.GetByIdAsync(serviceJob.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), serviceJob.ServiceOrderId);

        if (order.Status != ServiceStatus.Executing)
            throw new BusinessException("Jobs can only be started after the customer has approved the service.");

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to start progress on a job.");

        serviceJob.StartProgress(userId);
        _serviceJobRepository.Update(serviceJob);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceJobMapper.MapToDto(serviceJob);
    }
}

public class CompleteJobCommandHandler : IRequestHandler<CompleteJobCommand, ServiceJobDto>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;

    public CompleteJobCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IServiceOrderRepository serviceOrderRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService)
    {
        _serviceJobRepository = serviceJobRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
    }

    public async Task<ServiceJobDto> Handle(CompleteJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdWithServiceOrderAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        var order = serviceJob.ServiceOrder;

        if (order.Status != ServiceStatus.Executing)
            throw new BusinessException("Jobs can only be completed after the customer has approved the service.");

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to complete a job.");

        serviceJob.Complete(userId);
        _serviceJobRepository.Update(serviceJob);

        var finished = order.TryFinish();
        if (finished)
            _serviceOrderRepository.Update(order);

        await _unitOfWork.CommitAsync(cancellationToken);

        if (finished)
        {
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);
            if (customer is not null)
            {
                var body = await _emailTemplateService.RenderServiceFinishedAsync(order, customer);
                await _emailService.SendAsync(customer.Email, customer.Name,
                    "Your service has been completed", body, isHtml: true, cancellationToken);
            }
        }

        return ServiceJobMapper.MapToDto(serviceJob);
    }
}

internal static class ServiceJobMapper
{
    internal static ServiceJobDto MapToDto(ServiceJob serviceJob) =>
        new(serviceJob.Id,
            serviceJob.ServiceOrderId,
            serviceJob.Name,
            serviceJob.Description,
            serviceJob.UnitCost,
            serviceJob.Status.ToString(),
            serviceJob.AssignedUserId,
            serviceJob.CreatedAt,
            serviceJob.CreatedUserId,
            serviceJob.LastUpdatedUserId);
}
