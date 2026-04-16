using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.Vehicles.Queries;

public record GetVehicleByIdQuery(Guid Id) : IRequest<VehicleDto>;

public record GetAllVehiclesQuery : IRequest<IEnumerable<VehicleDto>>;

public record GetVehiclesByCustomerIdQuery(Guid CustomerId) : IRequest<IEnumerable<VehicleDto>>;
