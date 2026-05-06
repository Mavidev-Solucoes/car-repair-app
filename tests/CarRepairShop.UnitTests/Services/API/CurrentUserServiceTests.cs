using System.Security.Claims;
using CarRepairShop.API.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CarRepairShop.UnitTests.Services.API;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _service = new CurrentUserService(_httpContextAccessorMock.Object);
    }

    [Fact]
    public void UserId_NoHttpContext_ReturnsNull()
    {
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        Assert.Null(_service.UserId);
    }

    [Fact]
    public void UserId_NoUserClaims_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

        Assert.Null(_service.UserId);
    }

    [Fact]
    public void UserId_WithSubClaim_ReturnsGuid()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim("sub", userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

        var result = _service.UserId;

        Assert.Equal(userId, result);
    }

    [Fact]
    public void UserId_WithNameIdentifierClaim_ReturnsGuid()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

        var result = _service.UserId;

        Assert.Equal(userId, result);
    }

    [Fact]
    public void UserId_InvalidGuidInClaim_ReturnsNull()
    {
        var claims = new[] { new Claim("sub", "not-a-guid") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

        Assert.Null(_service.UserId);
    }
}
