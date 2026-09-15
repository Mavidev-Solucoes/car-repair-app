using System.Globalization;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;

namespace CarRepairShop.Services.Implementations;

public sealed class NewRelicBusinessTelemetry : IBusinessTelemetry
{
    private const string CorrelationIdItemKey = "CorrelationId";
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<NewRelicBusinessTelemetry> _logger;

    public NewRelicBusinessTelemetry(
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment hostEnvironment,
        ILogger<NewRelicBusinessTelemetry> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public Task RecordServiceOrderCreatedAsync(ServiceOrderCreatedTelemetry telemetry, CancellationToken cancellationToken)
    {
        try
        {
            var attributes = CreateBaseAttributes();
            attributes["ServiceOrderId"] = telemetry.ServiceOrderId.ToString();
            attributes["Status"] = telemetry.Status;
            attributes["CreatedAt"] = AsIsoUtc(telemetry.CreatedAt);

            NewRelic.Api.Agent.NewRelic.RecordCustomEvent(BusinessTelemetryEvents.ServiceOrderCreated, attributes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send {EventName} business event for service order {ServiceOrderId}.",
                BusinessTelemetryEvents.ServiceOrderCreated,
                telemetry.ServiceOrderId);
        }

        return Task.CompletedTask;
    }

    public Task RecordServiceOrderStatusChangedAsync(ServiceOrderStatusChangedTelemetry telemetry, CancellationToken cancellationToken)
    {
        try
        {
            var attributes = CreateBaseAttributes();
            attributes["ServiceOrderId"] = telemetry.ServiceOrderId.ToString();
            attributes["PreviousStatus"] = telemetry.PreviousStatus;
            attributes["NewStatus"] = telemetry.NewStatus;
            attributes["ChangedAt"] = AsIsoUtc(telemetry.ChangedAt);

            if (telemetry.DurationSeconds.HasValue)
                attributes["DurationSeconds"] = telemetry.DurationSeconds.Value;

            NewRelic.Api.Agent.NewRelic.RecordCustomEvent(BusinessTelemetryEvents.ServiceOrderStatusChanged, attributes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send {EventName} business event for service order {ServiceOrderId}.",
                BusinessTelemetryEvents.ServiceOrderStatusChanged,
                telemetry.ServiceOrderId);
        }

        return Task.CompletedTask;
    }

    private Dictionary<string, object> CreateBaseAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            ["Environment"] = _hostEnvironment.EnvironmentName
        };

        var correlationId = ResolveCorrelationId();
        if (!string.IsNullOrWhiteSpace(correlationId))
            attributes["CorrelationId"] = correlationId;

        return attributes;
    }

    private string? ResolveCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        if (httpContext.Items.TryGetValue(CorrelationIdItemKey, out var itemValue))
            return itemValue?.ToString();

        return httpContext.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();
    }

    private static string AsIsoUtc(DateTime value) =>
        (value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc))
        .ToString("O", CultureInfo.InvariantCulture);
}
