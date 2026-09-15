using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CarRepairShop.API.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string HttpContextItemKey = "CorrelationId";
    private const int MaxCorrelationIdLength = 128;
    private static readonly Regex CorrelationIdRegex = new(
        "^[A-Za-z0-9._:-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[HttpContextItemKey] = correlationId;
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        var activity = Activity.Current;
        activity?.SetTag("correlation.id", correlationId);
        activity?.AddBaggage("correlation.id", correlationId);

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = context.TraceIdentifier,
            ["TraceId"] = activity?.TraceId.ToString(),
            ["SpanId"] = activity?.SpanId.ToString()
        });

        await _next(context);
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var existing = values.FirstOrDefault();
            if (IsValidCorrelationId(existing))
                return existing!;
        }

        return Guid.NewGuid().ToString("D");
    }

    private static bool IsValidCorrelationId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Length > MaxCorrelationIdLength)
            return false;

        return CorrelationIdRegex.IsMatch(value);
    }
}
