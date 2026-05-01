using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.Vehicles.Commands;

public record CreateVehicleCommand(Guid CustomerId, string Brand, string Model, int Year, string LicensePlate, string? Color) : IRequest<VehicleDto>;

public record UpdateVehicleCommand(Guid Id, string Brand, string Model, int Year, string LicensePlate, string? Color) : IRequest<VehicleDto>;

public record DeleteVehicleCommand(Guid Id) : IRequest<Unit>;
