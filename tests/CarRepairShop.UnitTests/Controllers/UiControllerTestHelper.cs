using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

internal static class UiControllerTestHelper
{
    internal static ClaimsPrincipal CreateUser(string role = "Admin", Guid? userId = null, string? name = null)
    {
        userId ??= Guid.NewGuid();
        name ??= "Test User";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Sub, userId.Value.ToString()),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, "test@example.com")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
    }

    internal static (ControllerContext context, ITempDataDictionary tempData) CreateControllerContext(
        ClaimsPrincipal user,
        IServiceProvider? serviceProvider = null,
        bool htmxRequest = false)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user,
            RequestServices = serviceProvider ?? BuildDefaultServices()
        };

        if (htmxRequest)
            httpContext.Request.Headers["HX-Request"] = "true";

        var tempDataProvider = Mock.Of<ITempDataProvider>();
        var tempData = new TempDataDictionary(httpContext, tempDataProvider);

        var controllerContext = new ControllerContext { HttpContext = httpContext };
        return (controllerContext, tempData);
    }

    internal static IServiceProvider BuildServicesWithAuth(
        Mock<IAuthenticationService>? authServiceMock = null)
    {
        authServiceMock ??= new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string?>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        authServiceMock.Setup(x => x.SignOutAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string?>(),
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        authServiceMock.Setup(x => x.AuthenticateAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string?>()))
            .ReturnsAsync(AuthenticateResult.NoResult());

        var services = new ServiceCollection();
        services.AddSingleton(authServiceMock.Object);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildDefaultServices()
    {
        return new ServiceCollection().BuildServiceProvider();
    }
}
