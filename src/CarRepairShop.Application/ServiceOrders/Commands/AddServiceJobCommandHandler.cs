using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class AddServiceJobCommandHandler : IRequestHandler<AddServiceJobCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceJobRepository _serviceJobRepository;
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;
    private readonly IServiceOrderHistoryTracker _historyTracker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceOrderBusinessTelemetry _serviceOrderBusinessTelemetry;

    public AddServiceJobCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IServiceJobRepository serviceJobRepository,
        IServiceOrderJobRepository serviceOrderJobRepository,
        IServiceOrderHistoryTracker historyTracker,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IServiceOrderBusinessTelemetry serviceOrderBusinessTelemetry)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _serviceJobRepository = serviceJobRepository;
        _serviceOrderJobRepository = serviceOrderJobRepository;
        _historyTracker = historyTracker;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _serviceOrderBusinessTelemetry = serviceOrderBusinessTelemetry;
    }

    public async Task<ServiceOrderDto> Handle(AddServiceJobCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var jobCatalog = await _serviceJobRepository.GetByIdAsync(request.ServiceJobId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.ServiceJobId);

        var statusHistoryCount = order.StatusHistory.Count;
        var job = new ServiceOrderJob(order.Id, jobCatalog.Id, jobCatalog.Name, jobCatalog.Description, jobCatalog.Price, userId);
        order.AttachServiceJob(job, userId);
        await _serviceOrderJobRepository.AddAsync(job, cancellationToken);
        await _historyTracker.AddLatestAsync(order, statusHistoryCount, cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);
        await _serviceOrderBusinessTelemetry.RecordStatusChangesAsync(order, statusHistoryCount, cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}
