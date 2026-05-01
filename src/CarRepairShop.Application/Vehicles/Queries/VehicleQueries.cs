using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.Vehicles.Queries;

public record GetVehicleByIdQuery(Guid Id) : IRequest<VehicleDto>;

public record GetVehiclesQuery : IRequest<PagedResult<VehicleDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; } = false;

    // Filters
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public string? LicensePlate { get; init; }
}

public record GetVehiclesByCustomerIdQuery(Guid CustomerId) : IRequest<IEnumerable<VehicleDto>>;
