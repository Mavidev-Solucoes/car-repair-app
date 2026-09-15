namespace CarRepairShop.Domain.Interfaces.Services;

public static class BusinessTelemetryEvents
{
    public const string ServiceOrderCreated = "CarRepairServiceOrderCreated";
    public const string ServiceOrderStatusChanged = "CarRepairServiceOrderStatusChanged";
}

public sealed record ServiceOrderCreatedTelemetry(
    Guid ServiceOrderId,
    string Status,
    DateTime CreatedAt);

public sealed record ServiceOrderStatusChangedTelemetry(
    Guid ServiceOrderId,
    string PreviousStatus,
    string NewStatus,
    DateTime ChangedAt,
    double? DurationSeconds);

public interface IBusinessTelemetry
{
    Task RecordServiceOrderCreatedAsync(ServiceOrderCreatedTelemetry telemetry, CancellationToken cancellationToken);
    Task RecordServiceOrderStatusChangedAsync(ServiceOrderStatusChangedTelemetry telemetry, CancellationToken cancellationToken);
}
