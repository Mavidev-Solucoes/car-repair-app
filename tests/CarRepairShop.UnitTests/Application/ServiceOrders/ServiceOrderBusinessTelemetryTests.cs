using System.Reflection;
using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceOrders;

public class ServiceOrderBusinessTelemetryTests
{
    private readonly Mock<IBusinessTelemetry> _businessTelemetryMock = new();
    private readonly Mock<ILogger<ServiceOrderBusinessTelemetry>> _loggerMock = new();
    private readonly ServiceOrderBusinessTelemetry _service;

    public ServiceOrderBusinessTelemetryTests()
    {
        _service = new ServiceOrderBusinessTelemetry(_businessTelemetryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RecordCreatedAsync_RecordsServiceOrderCreatedEvent()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await _service.RecordCreatedAsync(order, CancellationToken.None);

        _businessTelemetryMock.Verify(t => t.RecordServiceOrderCreatedAsync(
            It.Is<ServiceOrderCreatedTelemetry>(e =>
                e.ServiceOrderId == order.Id &&
                e.Status == "Received" &&
                e.CreatedAt == DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordStatusChangesAsync_RecordsPreviousAndNewStatusWithDuration()
    {
        var userId = Guid.NewGuid();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        var startedAt = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);
        SetChangedAt(order.StatusHistory.Single(), startedAt);

        var previousHistoryCount = order.StatusHistory.Count;
        order.AddServiceItem(new ServiceOrderItem(order.Id, Guid.NewGuid(), "Oil filter", 50m, 1), userId);
        SetChangedAt(order.StatusHistory.Last(), startedAt.AddMinutes(5));

        await _service.RecordStatusChangesAsync(order, previousHistoryCount, CancellationToken.None);

        _businessTelemetryMock.Verify(t => t.RecordServiceOrderStatusChangedAsync(
            It.Is<ServiceOrderStatusChangedTelemetry>(e =>
                e.ServiceOrderId == order.Id &&
                e.PreviousStatus == "Received" &&
                e.NewStatus == "Diagnosing" &&
                e.ChangedAt == startedAt.AddMinutes(5) &&
                e.DurationSeconds == 300),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordCreatedAsync_WhenTelemetryProviderFails_DoesNotThrow()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _businessTelemetryMock
            .Setup(t => t.RecordServiceOrderCreatedAsync(It.IsAny<ServiceOrderCreatedTelemetry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("telemetry unavailable"));

        await _service.RecordCreatedAsync(order, CancellationToken.None);

        _businessTelemetryMock.Verify(t => t.RecordServiceOrderCreatedAsync(
            It.IsAny<ServiceOrderCreatedTelemetry>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static void SetChangedAt(ServiceStatusHistory history, DateTime changedAt)
    {
        typeof(ServiceStatusHistory)
            .GetProperty(nameof(ServiceStatusHistory.ChangedAt), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(history, changedAt);
    }
}
