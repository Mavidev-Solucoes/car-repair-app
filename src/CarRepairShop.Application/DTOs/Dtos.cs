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
    string PersonalId,
    string Email,
    string Telephone,
    bool IsActive,
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
    Guid ServiceItemId,
    string Description,
    decimal Price,
    int Quantity);

public record ServiceItemDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    Guid? CreatedUserId = null,
    Guid? LastUpdatedUserId = null);

public record ServiceOrderDto(
    Guid Id,
    Guid VehicleId,
    Guid CustomerId,
    Guid AssignedUserId,
    string Status,
    decimal TotalPrice,
    DateTime CreatedAt,
    IEnumerable<ServiceOrderItemDto> ServiceItems,
    IEnumerable<ServiceOrderJobDto> ServiceJobs,
    IEnumerable<ServiceStatusHistoryDto> StatusHistory);

public record LoginResponseDto(string Token, string Email, string Name, string Role);

public record ServiceJobDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    DateTime CreatedAt,
    Guid? CreatedUserId = null,
    Guid? LastUpdatedUserId = null,
    TimeSpan? AverageTimeInProgress = null);

public record ServiceOrderJobDto(
    Guid Id,
    Guid ServiceOrderId,
    Guid ServiceJobId,
    string Name,
    string Description,
    decimal Price,
    string Status,
    Guid? AssignedUserId,
    DateTime CreatedAt,
    Guid? CreatedUserId = null,
    Guid? LastUpdatedUserId = null);

public record ServiceOrderJobStatusHistoryDto(
    Guid Id,
    Guid ServiceOrderJobId,
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAt,
    Guid? ChangedByUserId,
    TimeSpan? TimeInPreviousStatus);

public record ServiceStatusHistoryDto(
    Guid Id,
    Guid ServiceOrderId,
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAt,
    Guid? ChangedByUserId);
