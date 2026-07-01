using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class RemoveServiceJobCommandHandler : IRequestHandler<RemoveServiceJobCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveServiceJobCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IServiceOrderJobRepository serviceOrderJobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _serviceOrderJobRepository = serviceOrderJobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(RemoveServiceJobCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("User must be authenticated.");

        var order = await _serviceOrderRepository.GetWithAllDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        order.RemoveServiceJob(request.ServiceJobId, userId);

        var job = await _serviceOrderJobRepository.GetByIdAsync(request.ServiceJobId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrderJob), request.ServiceJobId);

        _serviceOrderJobRepository.Delete(job);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}
