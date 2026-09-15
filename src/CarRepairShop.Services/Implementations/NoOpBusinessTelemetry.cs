using CarRepairShop.Domain.Interfaces.Services;

namespace CarRepairShop.Services.Implementations;

public sealed class NoOpBusinessTelemetry : IBusinessTelemetry
{
    public Task RecordServiceOrderCreatedAsync(ServiceOrderCreatedTelemetry telemetry, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task RecordServiceOrderStatusChangedAsync(ServiceOrderStatusChangedTelemetry telemetry, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
