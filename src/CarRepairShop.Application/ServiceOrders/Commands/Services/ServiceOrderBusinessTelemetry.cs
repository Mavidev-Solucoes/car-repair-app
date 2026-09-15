using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public class ServiceOrderBusinessTelemetry : IServiceOrderBusinessTelemetry
{
    private readonly IBusinessTelemetry _businessTelemetry;
    private readonly ILogger<ServiceOrderBusinessTelemetry> _logger;

    public ServiceOrderBusinessTelemetry(
        IBusinessTelemetry businessTelemetry,
        ILogger<ServiceOrderBusinessTelemetry> logger)
    {
        _businessTelemetry = businessTelemetry;
        _logger = logger;
    }

    public async Task RecordCreatedAsync(ServiceOrder order, CancellationToken cancellationToken)
    {
        try
        {
            await _businessTelemetry.RecordServiceOrderCreatedAsync(
                new ServiceOrderCreatedTelemetry(order.Id, order.Status.ToString(), AsUtc(order.CreatedAt)),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to record business telemetry for service order creation {ServiceOrderId}.",
                order.Id);
        }
    }

    public async Task RecordStatusChangesAsync(ServiceOrder order, int previousHistoryCount, CancellationToken cancellationToken)
    {
        var transitions = order.StatusHistory
            .Skip(previousHistoryCount)
            .Where(history => history.FromStatus.HasValue)
            .ToArray();

        foreach (var transition in transitions)
        {
            try
            {
                await _businessTelemetry.RecordServiceOrderStatusChangedAsync(
                    new ServiceOrderStatusChangedTelemetry(
                        order.Id,
                        transition.FromStatus!.Value.ToString(),
                        transition.ToStatus.ToString(),
                        AsUtc(transition.ChangedAt),
                        CalculateDurationSeconds(order, transition)),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to record business telemetry for service order status change {ServiceOrderId}.",
                    order.Id);
            }
        }
    }

    private static double? CalculateDurationSeconds(ServiceOrder order, ServiceStatusHistory transition)
    {
        if (!transition.FromStatus.HasValue)
            return null;

        var changedAt = AsUtc(transition.ChangedAt);
        var previousStatusStartedAt = order.StatusHistory
            .Where(history =>
                history.Id != transition.Id &&
                history.ToStatus == transition.FromStatus.Value &&
                AsUtc(history.ChangedAt) <= changedAt)
            .OrderByDescending(history => AsUtc(history.ChangedAt))
            .Select(history => (DateTime?)AsUtc(history.ChangedAt))
            .FirstOrDefault();

        if (!previousStatusStartedAt.HasValue)
            return null;

        var duration = changedAt - previousStatusStartedAt.Value;
        return duration.TotalSeconds >= 0 ? duration.TotalSeconds : null;
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
