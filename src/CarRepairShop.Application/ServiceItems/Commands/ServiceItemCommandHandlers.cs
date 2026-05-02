using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceItems.Commands;

public class CreateServiceItemCommandHandler : IRequestHandler<CreateServiceItemCommand, ServiceItemDto>
{
    private readonly IServiceItemRepository _serviceItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateServiceItemCommandHandler(
        IServiceItemRepository serviceItemRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceItemRepository = serviceItemRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceItemDto> Handle(CreateServiceItemCommand request, CancellationToken cancellationToken)
    {
        if (await _serviceItemRepository.ExistsByNameAsync(request.Name, cancellationToken))
            throw new BusinessException($"A service item with name '{request.Name}' already exists.");

        var serviceItem = new ServiceItem(
            request.Name,
            request.Description,
            request.Price,
            request.Stock,
            _currentUserService.UserId);

        await _serviceItemRepository.AddAsync(serviceItem, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceItemMapper.MapToDto(serviceItem);
    }
}

public class UpdateServiceItemCommandHandler : IRequestHandler<UpdateServiceItemCommand, ServiceItemDto>
{
    private readonly IServiceItemRepository _serviceItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateServiceItemCommandHandler(
        IServiceItemRepository serviceItemRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceItemRepository = serviceItemRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceItemDto> Handle(UpdateServiceItemCommand request, CancellationToken cancellationToken)
    {
        if (await _serviceItemRepository.ExistsByNameAsync(request.Name, request.Id, cancellationToken))
            throw new BusinessException($"A service item with name '{request.Name}' already exists.");

        var serviceItem = await _serviceItemRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceItem), request.Id);

        serviceItem.Update(request.Name, request.Description, request.Price, request.Stock, _currentUserService.UserId);
        _serviceItemRepository.Update(serviceItem);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceItemMapper.MapToDto(serviceItem);
    }
}

public class DeleteServiceItemCommandHandler : IRequestHandler<DeleteServiceItemCommand, Unit>
{
    private readonly IServiceItemRepository _serviceItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServiceItemCommandHandler(IServiceItemRepository serviceItemRepository, IUnitOfWork unitOfWork)
    {
        _serviceItemRepository = serviceItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteServiceItemCommand request, CancellationToken cancellationToken)
    {
        var serviceItem = await _serviceItemRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceItem), request.Id);

        _serviceItemRepository.Delete(serviceItem);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}

file static class ServiceItemMapper
{
    internal static ServiceItemDto MapToDto(ServiceItem serviceItem) =>
        new(serviceItem.Id, serviceItem.Name, serviceItem.Description, serviceItem.Price, serviceItem.Stock,
            serviceItem.CreatedAt, serviceItem.CreatedUserId, serviceItem.LastUpdatedUserId);
}
