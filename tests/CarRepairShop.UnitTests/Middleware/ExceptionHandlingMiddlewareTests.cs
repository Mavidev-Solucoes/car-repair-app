using CarRepairShop.API.Middleware;
using CarRepairShop.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarRepairShop.UnitTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock = new();

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNext()
    {
        var nextCalled = false;
        var middleware = new ExceptionHandlingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object);

        await middleware.InvokeAsync(CreateContext());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400()
    {
        var errors = new Dictionary<string, string[]> { { "Field", ["Error"] } };
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(errors),
            _loggerMock.Object);

        var context = CreateContext();
        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_Returns404()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("Entity", Guid.NewGuid()),
            _loggerMock.Object);

        var context = CreateContext();
        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BusinessException_Returns422()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new BusinessException("Business rule violated"),
            _loggerMock.Object);

        var context = CreateContext();
        await middleware.InvokeAsync(context);

        Assert.Equal(422, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_Returns422()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Invalid operation"),
            _loggerMock.Object);

        var context = CreateContext();
        await middleware.InvokeAsync(context);

        Assert.Equal(422, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new Exception("Something went wrong"),
            _loggerMock.Object);

        var context = CreateContext();
        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_WritesErrorsToBody()
    {
        var errors = new Dictionary<string, string[]> { { "Name", ["Name is required."] } };
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(errors),
            _loggerMock.Object);

        var context = CreateContext();
        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("errors", body);
    }
}
