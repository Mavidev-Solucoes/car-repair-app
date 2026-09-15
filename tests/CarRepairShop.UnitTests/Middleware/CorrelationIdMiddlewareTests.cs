using CarRepairShop.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarRepairShop.UnitTests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private readonly Mock<ILogger<CorrelationIdMiddleware>> _loggerMock = new();

    private CorrelationIdMiddleware CreateMiddleware() =>
        new(_ => Task.CompletedTask, _loggerMock.Object);

    [Fact]
    public async Task InvokeAsync_WithValidHeader_PreservesCorrelationId()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "abc-DEF_123.:id";

        await middleware.InvokeAsync(context);

        Assert.Equal("abc-DEF_123.:id", context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
    }

    [Fact]
    public async Task InvokeAsync_WithoutHeader_GeneratesUuid()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParseExact(correlationId, "D", out _));
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidHeader_GeneratesUuid()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "invalid value";

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParseExact(correlationId, "D", out _));
    }

    [Fact]
    public async Task InvokeAsync_WithTooLongHeader_GeneratesUuid()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = new string('a', 129);

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParseExact(correlationId, "D", out _));
    }

    [Fact]
    public async Task InvokeAsync_AddsCorrelationIdToResponseHeader()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var headerValue));
        Assert.Equal(context.Items[CorrelationIdMiddleware.HttpContextItemKey], headerValue.ToString());
    }

    [Fact]
    public async Task InvokeAsync_SetsTraceIdentifierToResolvedCorrelationId()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "trace-id_123";

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
        Assert.Equal(correlationId, context.TraceIdentifier);
    }
}
