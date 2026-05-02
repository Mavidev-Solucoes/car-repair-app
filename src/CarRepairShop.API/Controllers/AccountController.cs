using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRepairShop.API.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IUiApiClient _apiClient;

    public AccountController(IUiApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(GetLandingPath(User));

        return Redirect("/ui/login");
    }

    [HttpGet("/ui")]
    public IActionResult UiRoot()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(GetLandingPath(User));

        return Redirect("/ui/login");
    }

    [HttpGet("/ui/login")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(GetLandingPath(User));

        return View(new LoginViewModel());
    }

    [HttpPost("/ui/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        LoginResponseDto response;
        try
        {
            response = await _apiClient.PostAsync<object, LoginResponseDto>(
                "/api/auth/login",
                new { model.Email, model.Password },
                cancellationToken);
        }
        catch (UiApiException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        var subject = jwt.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub)?.Value;
        var role = jwt.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role || claim.Type == "role")?.Value ?? response.Role;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, response.Email),
            new(ClaimTypes.Email, response.Email),
            new(JwtRegisteredClaimNames.Name, response.Name),
            new(ClaimTypes.Name, response.Name),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = response.Token }
        ]);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        return Redirect(GetLandingPath(new ClaimsPrincipal(identity)));
    }

    [HttpPost("/ui/logout")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/ui/login");
    }

    [HttpGet("/ui/password")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public IActionResult Password()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost("/ui/password")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Password(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            model.Flash = new FlashMessageViewModel { Message = "Current user could not be resolved.", IsError = true };
            return View(model);
        }

        try
        {
            await _apiClient.PatchAsync(
                $"/api/users/{parsedUserId}/change-password",
                new { Id = parsedUserId, model.CurrentPassword, model.NewPassword },
                cancellationToken);

            model = new ChangePasswordViewModel
            {
                Flash = new FlashMessageViewModel { Message = "Password updated." }
            };
            return View(model);
        }
        catch (UiApiException ex)
        {
            if (ex.Errors is { Count: > 0 })
            {
                foreach (var (key, messages) in ex.Errors)
                {
                    var modelKey = string.IsNullOrWhiteSpace(key) ? string.Empty : key;
                    foreach (var message in messages)
                        ModelState.AddModelError(modelKey, message);
                }
            }
            else
            {
                model.Flash = new FlashMessageViewModel { Message = ex.Message, IsError = true };
            }

            return View(model);
        }
    }

    private static string GetLandingPath(ClaimsPrincipal user)
    {
        if (user.IsInRole("Customer"))
            return "/ui/services";

        if (user.IsInRole("Mechanic"))
            return "/ui/order-jobs";

        return "/ui/services";
    }
}
