using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Application.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);

public record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Document,
    DateTime CreatedAt);

public record VehicleDto(
    Guid Id,
    Guid CustomerId,
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    string? Color,
    DateTime CreatedAt);

public record ServiceItemDto(
    Guid Id,
    Guid ServiceOrderId,
    string Description,
    decimal Price,
    int Quantity);

public record ServiceOrderDto(
    Guid Id,
    Guid VehicleId,
    string Description,
    string Status,
    decimal TotalPrice,
    DateTime? CompletedAt,
    string? Notes,
    DateTime CreatedAt,
    IEnumerable<ServiceItemDto> ServiceItems);

public record LoginResponseDto(string Token, string Email, string Name, string Role);
