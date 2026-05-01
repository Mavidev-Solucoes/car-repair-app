using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceJobs.Commands;

public class CreateServiceJobCommandHandler : IRequestHandler<CreateServiceJobCommand, ServiceJobDto>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateServiceJobCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceJobDto> Handle(CreateServiceJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = new ServiceJob(
            request.Name,
            request.Description,
            request.UnitCost,
            _currentUserService.UserId);

        await _serviceJobRepository.AddAsync(serviceJob, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceJobMapper.MapToDto(serviceJob);
    }
}

public class UpdateServiceJobCommandHandler : IRequestHandler<UpdateServiceJobCommand, ServiceJobDto>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateServiceJobCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceJobDto> Handle(UpdateServiceJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        if (serviceJob.Status != Domain.Enums.JobStatus.Open)
            throw new BusinessException("Only Open jobs can be updated.");

        serviceJob.Update(request.Name, request.Description, request.UnitCost, _currentUserService.UserId);
        _serviceJobRepository.Update(serviceJob);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceJobMapper.MapToDto(serviceJob);
    }
}

public class DeleteServiceJobCommandHandler : IRequestHandler<DeleteServiceJobCommand, Unit>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServiceJobCommandHandler(IServiceJobRepository serviceJobRepository, IUnitOfWork unitOfWork)
    {
        _serviceJobRepository = serviceJobRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteServiceJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        _serviceJobRepository.Delete(serviceJob);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}

public class AcknowledgeJobCommandHandler : IRequestHandler<AcknowledgeJobCommand, ServiceJobDto>
{
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AcknowledgeJobCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceJobDto> Handle(AcknowledgeJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdWithHistoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public StartJobProgressCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceJobDto> Handle(StartJobProgressCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdWithHistoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CompleteJobCommandHandler(
        IServiceJobRepository serviceJobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceJobRepository = serviceJobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceJobDto> Handle(CompleteJobCommand request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdWithHistoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated to complete a job.");

        serviceJob.Complete(userId);
        _serviceJobRepository.Update(serviceJob);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceJobMapper.MapToDto(serviceJob);
    }
}

file static class ServiceJobMapper
{
    internal static ServiceJobDto MapToDto(ServiceJob serviceJob) =>
        new(serviceJob.Id,
            serviceJob.Name,
            serviceJob.Description,
            serviceJob.UnitCost,
            serviceJob.Status.ToString(),
            serviceJob.AssignedUserId,
            serviceJob.CreatedAt,
            serviceJob.CreatedUserId,
            serviceJob.LastUpdatedUserId);
}
