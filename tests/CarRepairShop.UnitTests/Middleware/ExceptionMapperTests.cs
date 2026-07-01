using System.Net;
using CarRepairShop.API.Middleware.ExceptionMappers;
using CarRepairShop.Application.Common.Exceptions;

namespace CarRepairShop.UnitTests.Middleware;

public class ExceptionMapperTests
{
    // ─── ValidationExceptionMapper ────────────────────────────────────────────

    [Fact]
    public void ValidationMapper_CanMap_ValidationException_ReturnsTrue()
    {
        var mapper = new ValidationExceptionMapper();
        Assert.True(mapper.CanMap(new ValidationException(new Dictionary<string, string[]>())));
    }

    [Fact]
    public void ValidationMapper_CanMap_OtherException_ReturnsFalse()
    {
        var mapper = new ValidationExceptionMapper();
        Assert.False(mapper.CanMap(new Exception("other")));
    }

    [Fact]
    public void ValidationMapper_Map_Returns400WithErrors()
    {
        var errors = new Dictionary<string, string[]> { { "Field", ["Error"] } };
        var mapper = new ValidationExceptionMapper();

        var (statusCode, response) = mapper.Map(new ValidationException(errors));

        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
        Assert.Equal(400, response.Status);
        Assert.NotNull(response.Errors);
        Assert.Contains("Field", response.Errors!.Keys);
    }

    // ─── NotFoundExceptionMapper ──────────────────────────────────────────────

    [Fact]
    public void NotFoundMapper_CanMap_NotFoundException_ReturnsTrue()
    {
        var mapper = new NotFoundExceptionMapper();
        Assert.True(mapper.CanMap(new NotFoundException("Entity", Guid.NewGuid())));
    }

    [Fact]
    public void NotFoundMapper_CanMap_OtherException_ReturnsFalse()
    {
        var mapper = new NotFoundExceptionMapper();
        Assert.False(mapper.CanMap(new Exception("other")));
    }

    [Fact]
    public void NotFoundMapper_Map_Returns404()
    {
        var mapper = new NotFoundExceptionMapper();
        var ex = new NotFoundException("Order", Guid.NewGuid());

        var (statusCode, response) = mapper.Map(ex);

        Assert.Equal(HttpStatusCode.NotFound, statusCode);
        Assert.Equal(404, response.Status);
        Assert.Equal("Not Found", response.Title);
    }

    // ─── BusinessExceptionMapper ──────────────────────────────────────────────

    [Fact]
    public void BusinessMapper_CanMap_BusinessException_ReturnsTrue()
    {
        var mapper = new BusinessExceptionMapper();
        Assert.True(mapper.CanMap(new BusinessException("rule")));
    }

    [Fact]
    public void BusinessMapper_CanMap_OtherException_ReturnsFalse()
    {
        var mapper = new BusinessExceptionMapper();
        Assert.False(mapper.CanMap(new Exception("other")));
    }

    [Fact]
    public void BusinessMapper_Map_Returns422()
    {
        var mapper = new BusinessExceptionMapper();
        var ex = new BusinessException("Business rule violated");

        var (statusCode, response) = mapper.Map(ex);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, statusCode);
        Assert.Equal(422, response.Status);
        Assert.Equal("Business Rule Violation", response.Title);
        Assert.Equal(ex.Message, response.Detail);
    }

    // ─── InvalidOperationExceptionMapper ─────────────────────────────────────

    [Fact]
    public void InvalidOperationMapper_CanMap_InvalidOperationException_ReturnsTrue()
    {
        var mapper = new InvalidOperationExceptionMapper();
        Assert.True(mapper.CanMap(new InvalidOperationException("ops")));
    }

    [Fact]
    public void InvalidOperationMapper_CanMap_OtherException_ReturnsFalse()
    {
        var mapper = new InvalidOperationExceptionMapper();
        Assert.False(mapper.CanMap(new Exception("other")));
    }

    [Fact]
    public void InvalidOperationMapper_Map_Returns422()
    {
        var mapper = new InvalidOperationExceptionMapper();
        var ex = new InvalidOperationException("bad state");

        var (statusCode, response) = mapper.Map(ex);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, statusCode);
        Assert.Equal(422, response.Status);
        Assert.Equal("Business Rule Violation", response.Title);
        Assert.Equal(ex.Message, response.Detail);
    }
}
