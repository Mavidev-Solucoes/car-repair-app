using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace CarRepairShop.API.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class UsersUiController : Controller
{
    private readonly IUiApiClient _apiClient;

    public UsersUiController(IUiApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/ui/users")]
    public async Task<IActionResult> Index([Bind(Prefix = "Filters")][FromQuery] UserListFiltersViewModel filters, CancellationToken cancellationToken)
    {
        return View(await BuildPageAsync(filters, cancellationToken));
    }

    [HttpGet("/ui/users/{id:guid}/details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var user = await _apiClient.GetAsync<UserDto>($"/api/users/{id}", cancellationToken);
        return PartialView("_UserDetailsModalContent", BuildModalViewModel(user));
    }

    [HttpPost("/ui/users/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind(Prefix = "Form")] UserFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || (!form.Id.HasValue && string.IsNullOrWhiteSpace(form.Password)))
        {
            if (!form.Id.HasValue && string.IsNullOrWhiteSpace(form.Password))
                ModelState.AddModelError("Form.Password", "Password is required for new users.");

            form.Id = null;
            return View("Index", await BuildPageAsync(new UserListFiltersViewModel(), cancellationToken, form));
        }

        try
        {
            var result = await _apiClient.PostAsync<object, UserDto>(
                "/api/users",
                new { form.Name, form.Email, Password = form.Password!, form.Role },
                cancellationToken);
            SetFlash("User created.");

            return Redirect("/ui/users");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            form.Id = null;
            return View("Index", await BuildPageAsync(new UserListFiltersViewModel(), cancellationToken, form));
        }
    }

    [HttpPost("/ui/users/{id:guid}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Form")] UserFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = id;
        form.Password = null;

        if (!ModelState.IsValid)
            return PartialView("_UserDetailsModalContent", new UserDetailsModalViewModel { Form = form, IsActive = true });

        try
        {
            var user = await _apiClient.GetAsync<UserDto>($"/api/users/{id}", cancellationToken);
            await _apiClient.PutAsync<object, object>(
                $"/api/users/{id}",
                new { Id = id, form.Name, form.Email, form.Role },
                cancellationToken);
            SetFlash("User updated.");
            return HtmxRedirect("/ui/users");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            var user = await _apiClient.GetAsync<UserDto>($"/api/users/{id}", cancellationToken);
            return PartialView("_UserDetailsModalContent", new UserDetailsModalViewModel { Form = form, IsActive = user.IsActive });
        }
    }

    [HttpPost("/ui/users/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteAsync($"/api/users/{id}", cancellationToken);
            SetFlash("User deactivated.");
            return Redirect("/ui/users");
        }
        catch (UiApiException ex)
        {
            SetFlash(ex.Message, isError: true);
            return Redirect("/ui/users");
        }
    }

    [HttpPost("/ui/users/{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.PatchAsync($"/api/users/{id}/activate", cancellationToken);
            SetFlash("User activated.");
            return Redirect("/ui/users");
        }
        catch (UiApiException ex)
        {
            SetFlash(ex.Message, isError: true);
            return Redirect("/ui/users");
        }
    }

    private async Task<UsersPageViewModel> BuildPageAsync(UserListFiltersViewModel filters, CancellationToken cancellationToken, UserFormViewModel? formOverride = null)
    {
        var userPage = await _apiClient.GetAsync<PagedResult<UserDto>>(BuildUsersPath(filters), cancellationToken);
        var users = userPage.Items
            .OrderBy(user => user.Name)
            .Select(user => new UserListItemViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive
            })
            .ToList();

        return new UsersPageViewModel
        {
            Flash = TakeFlash(),
            Filters = filters,
            Form = formOverride ?? new UserFormViewModel(),
            Users = users
        };
    }

    private static UserDetailsModalViewModel BuildModalViewModel(UserDto user)
    {
        return new UserDetailsModalViewModel
        {
            Form = new UserFormViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            },
            IsActive = user.IsActive
        };
    }

    private static string BuildUsersPath(UserListFiltersViewModel filters)
    {
        return QueryHelpers.AddQueryString("/api/users", new Dictionary<string, string?>
        {
            ["page"] = "1",
            ["pageSize"] = "100",
            ["search"] = filters.Search,
            ["role"] = filters.Role?.ToString(),
            ["isActive"] = filters.IsActive?.ToString()?.ToLowerInvariant()
        });
    }

    private void SetFlash(string message, bool isError = false)
    {
        TempData["FlashMessage"] = message;
        TempData["FlashIsError"] = isError;
    }

    private FlashMessageViewModel? TakeFlash()
    {
        if (TempData["FlashMessage"] is not string message || string.IsNullOrWhiteSpace(message))
            return null;

        var isError = TempData["FlashIsError"] is bool value && value;
        return new FlashMessageViewModel { Message = message, IsError = isError };
    }

    private void AddApiErrors(UiApiException exception, string prefix)
    {
        if (exception.Errors is null || exception.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        foreach (var (key, messages) in exception.Errors)
        {
            var modelKey = string.IsNullOrWhiteSpace(key) ? string.Empty : $"{prefix}.{key}";
            foreach (var message in messages)
                ModelState.AddModelError(modelKey, message);
        }
    }

    private IActionResult HtmxRedirect(string url)
    {
        if (Request.Headers.TryGetValue("HX-Request", out var values) && values.Count > 0 && values[0] == "true")
        {
            Response.Headers["HX-Redirect"] = url;
            return new EmptyResult();
        }

        return Redirect(url);
    }
}
