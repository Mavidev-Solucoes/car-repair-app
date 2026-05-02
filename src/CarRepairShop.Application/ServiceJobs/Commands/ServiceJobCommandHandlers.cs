using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
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
        if (await _serviceJobRepository.ExistsByNameAsync(request.Name, cancellationToken))
            throw new BusinessException($"A service job with name '{request.Name}' already exists.");

        var serviceJob = new ServiceJob(request.Name, request.Description, request.Price, _currentUserService.UserId);
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

        serviceJob.Update(request.Name, request.Description, request.Price, _currentUserService.UserId);
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

internal static class ServiceJobMapper
{
    internal static ServiceJobDto MapToDto(ServiceJob serviceJob) =>
        new(serviceJob.Id, serviceJob.Name, serviceJob.Description, serviceJob.Price,
            serviceJob.CreatedAt, serviceJob.CreatedUserId, serviceJob.LastUpdatedUserId);
}
