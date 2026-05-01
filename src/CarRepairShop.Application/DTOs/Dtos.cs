using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Application.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    UserType UserType,
    bool IsActive,
    DateTime CreatedAt);

public record CustomerDto(
    Guid Id,
    string Name,
    string PersonalId,
    string Email,
    string Telephone,
    DateTime CreatedAt,
    Guid? CreatedUserId = null,
    Guid? LastUpdatedUserId = null);

public record VehicleDto(
    Guid Id,
    Guid CustomerId,
    string Brand,
    string Model,
    int Year,
    string LicensePlate,
    string? Color,
    DateTime CreatedAt);

public record ServiceOrderItemDto(
    Guid Id,
    Guid ServiceOrderId,
    string Description,
    decimal Price,
    int Quantity);

public record ServiceItemDto(
    Guid Id,
    string Name,
    string Description,
    int UnitCost,
    DateTime CreatedAt,
    Guid? CreatedUserId = null,
    Guid? LastUpdatedUserId = null);

public record ServiceOrderDto(
    Guid Id,
    Guid VehicleId,
    string Description,
    string Status,
    decimal TotalPrice,
    DateTime? CompletedAt,
    string? Notes,
    DateTime CreatedAt,
    IEnumerable<ServiceOrderItemDto> ServiceItems);

public record LoginResponseDto(string Token, string Email, string Name, string Role);

public record ServiceJobDto(
    Guid Id,
    string Name,
    string Description,
    int UnitCost,
    string Status,
    Guid? AssignedUserId,
    DateTime CreatedAt,
    Guid? CreatedUserId = null,
    Guid? LastUpdatedUserId = null);

public record ServiceJobStatusHistoryDto(
    Guid Id,
    Guid ServiceJobId,
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAt,
    Guid? ChangedByUserId,
    TimeSpan? TimeInPreviousStatus);
