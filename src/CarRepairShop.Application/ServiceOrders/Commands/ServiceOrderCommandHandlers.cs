using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class CreateServiceOrderCommandHandler : IRequestHandler<CreateServiceOrderCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateServiceOrderCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IVehicleRepository vehicleRepository,
        IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderDto> Handle(CreateServiceOrderCommand request, CancellationToken cancellationToken)
    {
        _ = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        var order = new ServiceOrder(request.VehicleId, request.Description, request.Notes);
        await _serviceOrderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

public class UpdateServiceOrderCommandHandler : IRequestHandler<UpdateServiceOrderCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServiceOrderCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderDto> Handle(UpdateServiceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithItemsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        order.UpdateDescription(request.Description, request.Notes);
        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

public class UpdateServiceOrderStatusCommandHandler : IRequestHandler<UpdateServiceOrderStatusCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServiceOrderStatusCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderDto> Handle(UpdateServiceOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithItemsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        order.UpdateStatus(request.Status);
        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

public class AddServiceItemCommandHandler : IRequestHandler<AddServiceItemCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddServiceItemCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderDto> Handle(AddServiceItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithItemsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var item = new ServiceOrderItem(order.Id, request.Description, request.Price, request.Quantity);
        order.AddServiceItem(item);
        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

public class RemoveServiceItemCommandHandler : IRequestHandler<RemoveServiceItemCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveServiceItemCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderDto> Handle(RemoveServiceItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetWithItemsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        order.RemoveServiceItem(request.ServiceItemId);
        _serviceOrderRepository.Update(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapper.MapToDto(order);
    }
}

public class DeleteServiceOrderCommandHandler : IRequestHandler<DeleteServiceOrderCommand, Unit>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServiceOrderCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteServiceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        _serviceOrderRepository.Delete(order);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}

file static class ServiceOrderMapper
{
    public static ServiceOrderDto MapToDto(ServiceOrder order)
    {
        var items = order.ServiceItems.Select(i => new ServiceOrderItemDto(i.Id, i.ServiceOrderId, i.Description, i.Price, i.Quantity));
        return new ServiceOrderDto(order.Id, order.VehicleId, order.Description, order.Status.ToString(), order.TotalPrice, order.CompletedAt, order.Notes, order.CreatedAt, items);
    }
}
