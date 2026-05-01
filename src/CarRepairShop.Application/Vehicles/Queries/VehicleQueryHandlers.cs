using System.Linq.Expressions;
using CarRepairShop.Application.Common;
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

        return VehicleQueryMapper.MapToDto(vehicle);
    }
}

public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, PagedResult<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;

    public GetVehiclesQueryHandler(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<PagedResult<VehicleDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        var filters = BuildFilters(request);

        var (items, totalCount) = await _vehicleRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        return new PagedResult<VehicleDto>(
            items.Select(VehicleQueryMapper.MapToDto),
            totalCount,
            request.Page,
            request.PageSize);
    }

    private static IEnumerable<Expression<Func<Vehicle, bool>>> BuildFilters(GetVehiclesQuery request)
    {
        var filters = new List<Expression<Func<Vehicle, bool>>>();

        if (!string.IsNullOrWhiteSpace(request.Brand))
            filters.Add(v => v.Brand.Contains(request.Brand));

        if (!string.IsNullOrWhiteSpace(request.Model))
            filters.Add(v => v.Model.Contains(request.Model));

        if (request.Year.HasValue)
            filters.Add(v => v.Year == request.Year.Value);

        if (!string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            var stripped = new string(request.LicensePlate.Where(c => c != '-').ToArray()).ToUpperInvariant();
            filters.Add(v => v.LicensePlate.Contains(stripped));
        }

        return filters;
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
        return vehicles.Select(VehicleQueryMapper.MapToDto);
    }
}

file static class VehicleQueryMapper
{
    internal static VehicleDto MapToDto(Vehicle vehicle) =>
        new(vehicle.Id, vehicle.CustomerId, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate, vehicle.Color, vehicle.CreatedAt);
}
