using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarRepairShop.API.Controllers;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IUiApiClient> _apiClientMock = new();

    private static string CreateTestJwt(Guid userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-test-key-min-32-chars!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: new Claim[]
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(ClaimTypes.Role, role)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void Index_Unauthenticated_RedirectsToLogin()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.Index();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/login", redirect.Url);
    }

    [Fact]
    public void Index_Authenticated_Admin_RedirectsToServices()
    {
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.Index();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/services", redirect.Url);
    }

    [Fact]
    public void Index_Authenticated_Customer_RedirectsToServices()
    {
        var user = UiControllerTestHelper.CreateUser("Customer");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.Index();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/services", redirect.Url);
    }

    [Fact]
    public void Index_Authenticated_Mechanic_RedirectsToOrderJobs()
    {
        var user = UiControllerTestHelper.CreateUser("Mechanic");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.Index();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/order-jobs", redirect.Url);
    }

    [Fact]
    public void UiRoot_Unauthenticated_RedirectsToLogin()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.UiRoot();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/login", redirect.Url);
    }

    [Fact]
    public void UiRoot_Authenticated_RedirectsToLanding()
    {
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.UiRoot();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/services", redirect.Url);
    }

    [Fact]
    public void Login_Get_Unauthenticated_ReturnsView()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.Login();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<LoginViewModel>(view.Model);
    }

    [Fact]
    public void Login_Get_Authenticated_RedirectsToLanding()
    {
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.Login();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/services", redirect.Url);
    }

    [Fact]
    public async Task Login_Post_InvalidModel_ReturnsView()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };
        controller.ModelState.AddModelError("Email", "Required");

        var model = new LoginViewModel { Email = "", Password = "" };
        var result = await controller.Login(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, view.Model);
    }

    [Fact]
    public async Task Login_Post_ApiException_ReturnsViewWithError()
    {
        _apiClientMock.Setup(c => c.PostAsync<object, LoginResponseDto>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UiApiException("Invalid credentials", 401));

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var model = new LoginViewModel { Email = "user@example.com", Password = "password" };
        var result = await controller.Login(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var viewModel = Assert.IsType<LoginViewModel>(view.Model);
        Assert.Equal("Invalid credentials", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Login_Post_ValidCredentials_SignsInAndRedirects()
    {
        var userId = Guid.NewGuid();
        var token = CreateTestJwt(userId, "Admin");
        var response = new LoginResponseDto(token, "user@example.com", "Admin User", "Admin");

        _apiClientMock.Setup(c => c.PostAsync<object, LoginResponseDto>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string?>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(authServiceMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user, serviceProvider);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var model = new LoginViewModel { Email = "user@example.com", Password = "password" };
        var result = await controller.Login(model, CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/services", redirect.Url);
    }

    [Fact]
    public async Task Logout_SignsOutAndRedirects()
    {
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.SignOutAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(authServiceMock.Object);

        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user, services.BuildServiceProvider());
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = await controller.Logout();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/login", redirect.Url);
    }

    [Fact]
    public void Password_Get_ReturnsView()
    {
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = controller.Password();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ChangePasswordViewModel>(view.Model);
    }

    [Fact]
    public async Task Password_Post_InvalidModel_ReturnsView()
    {
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };
        controller.ModelState.AddModelError("CurrentPassword", "Required");

        var model = new ChangePasswordViewModel();
        var result = await controller.Password(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, view.Model);
    }

    [Fact]
    public async Task Password_Post_InvalidUserId_ReturnsViewWithError()
    {
        // User without valid Sub claim
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid")
        }, "Test"));
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "old",
            NewPassword = "new",
            ConfirmPassword = "new"
        };
        var result = await controller.Password(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ChangePasswordViewModel>(view.Model);
        Assert.NotNull(vm.Flash);
        Assert.True(vm.Flash!.IsError);
    }

    [Fact]
    public async Task Password_Post_Success_ReturnsViewWithSuccessFlash()
    {
        var userId = Guid.NewGuid();
        var user = UiControllerTestHelper.CreateUser("Admin", userId);
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);

        _apiClientMock.Setup(c => c.PatchAsync(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "old",
            NewPassword = "new",
            ConfirmPassword = "new"
        };
        var result = await controller.Password(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ChangePasswordViewModel>(view.Model);
        Assert.NotNull(vm.Flash);
        Assert.Equal("Password updated.", vm.Flash!.Message);
    }

    [Fact]
    public async Task Password_Post_ApiException_WithErrors_AddsModelErrors()
    {
        var userId = Guid.NewGuid();
        var user = UiControllerTestHelper.CreateUser("Admin", userId);
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);

        var errors = new Dictionary<string, string[]>
        {
            { "CurrentPassword", new[] { "Incorrect password." } }
        };
        _apiClientMock.Setup(c => c.PatchAsync(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UiApiException("Validation failed", 422, errors));

        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "wrong",
            NewPassword = "new",
            ConfirmPassword = "new"
        };
        var result = await controller.Password(model, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Password_Post_ApiException_NoErrors_SetsFlash()
    {
        var userId = Guid.NewGuid();
        var user = UiControllerTestHelper.CreateUser("Admin", userId);
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);

        _apiClientMock.Setup(c => c.PatchAsync(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UiApiException("Server error", 500));

        var controller = new AccountController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "old",
            NewPassword = "new",
            ConfirmPassword = "new"
        };
        var result = await controller.Password(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ChangePasswordViewModel>(view.Model);
        Assert.NotNull(vm.Flash);
        Assert.True(vm.Flash!.IsError);
        Assert.Equal("Server error", vm.Flash.Message);
    }
}
