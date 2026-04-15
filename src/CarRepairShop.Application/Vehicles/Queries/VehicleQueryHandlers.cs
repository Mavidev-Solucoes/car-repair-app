using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.Vehicles.Queries;

public class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, VehicleDto>
{
    private readonly IVehicleRepository _vehicleRepository;

    public GetVehicleByIdQueryHandler(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<VehicleDto> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.Id);

        return new VehicleDto(vehicle.Id, vehicle.CustomerId, vehicle.Make, vehicle.Model, vehicle.Year, vehicle.LicensePlate, vehicle.Color, vehicle.CreatedAt);
    }
}

public class GetAllVehiclesQueryHandler : IRequestHandler<GetAllVehiclesQuery, IEnumerable<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;

    public GetAllVehiclesQueryHandler(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<IEnumerable<VehicleDto>> Handle(GetAllVehiclesQuery request, CancellationToken cancellationToken)
    {
        var vehicles = await _vehicleRepository.GetAllAsync(cancellationToken);
        return vehicles.Select(v => new VehicleDto(v.Id, v.CustomerId, v.Make, v.Model, v.Year, v.LicensePlate, v.Color, v.CreatedAt));
    }
}

public class GetVehiclesByCustomerIdQueryHandler : IRequestHandler<GetVehiclesByCustomerIdQuery, IEnumerable<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;

    public GetVehiclesByCustomerIdQueryHandler(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<IEnumerable<VehicleDto>> Handle(GetVehiclesByCustomerIdQuery request, CancellationToken cancellationToken)
    {
        var vehicles = await _vehicleRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        return vehicles.Select(v => new VehicleDto(v.Id, v.CustomerId, v.Make, v.Model, v.Year, v.LicensePlate, v.Color, v.CreatedAt));
    }
}
