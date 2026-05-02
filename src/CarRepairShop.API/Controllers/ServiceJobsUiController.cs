using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace CarRepairShop.API.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class ServiceJobsUiController : Controller
{
    private readonly IUiApiClient _apiClient;

    public ServiceJobsUiController(IUiApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/ui/service-jobs")]
    public async Task<IActionResult> Index([Bind(Prefix = "Filters")][FromQuery] CatalogListFiltersViewModel filters, CancellationToken cancellationToken)
    {
        return View(await BuildPageAsync(filters, cancellationToken));
    }

    [HttpGet("/ui/service-jobs/{id:guid}/details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var job = await _apiClient.GetAsync<ServiceJobDto>($"/api/service-jobs/{id}", cancellationToken);

        return PartialView("_ServiceJobDetailsModalContent", new ServiceJobDetailsModalViewModel
        {
            Form = new ServiceJobFormViewModel
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description,
                Price = job.Price
            },
            CanDelete = User.IsInRole("Admin")
        });
    }

    [HttpPost("/ui/service-jobs/save")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind(Prefix = "Form")] ServiceJobFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.Id = Guid.Empty;
            return View("Index", await BuildPageAsync(new CatalogListFiltersViewModel(), cancellationToken, form));
        }

        try
        {
            await _apiClient.PostAsync<object, ServiceJobDto>(
                "/api/service-jobs",
                new { form.Name, form.Description, form.Price },
                cancellationToken);
            SetFlash("Service job created.");

            return Redirect("/ui/service-jobs");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            form.Id = Guid.Empty;
            return View("Index", await BuildPageAsync(new CatalogListFiltersViewModel(), cancellationToken, form));
        }
    }

    [HttpPost("/ui/service-jobs/{id:guid}/update")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Form")] ServiceJobFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = id;

        if (!ModelState.IsValid)
            return PartialView("_ServiceJobDetailsModalContent", new ServiceJobDetailsModalViewModel { Form = form, CanDelete = User.IsInRole("Admin") });

        try
        {
            await _apiClient.PutAsync<object, object>(
                $"/api/service-jobs/{id}",
                new { Id = id, form.Name, form.Description, form.Price },
                cancellationToken);
            SetFlash("Service job updated.");
            return HtmxRedirect("/ui/service-jobs");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            return PartialView("_ServiceJobDetailsModalContent", new ServiceJobDetailsModalViewModel { Form = form, CanDelete = User.IsInRole("Admin") });
        }
    }

    [HttpPost("/ui/service-jobs/{id:guid}/delete")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteAsync($"/api/service-jobs/{id}", cancellationToken);
            SetFlash("Service job deleted.");
            return Redirect("/ui/service-jobs");
        }
        catch (UiApiException ex)
        {
            SetFlash(ex.Message, true);
            return Redirect("/ui/service-jobs");
        }
    }

    private async Task<ServiceJobsPageViewModel> BuildPageAsync(
        CatalogListFiltersViewModel filters,
        CancellationToken cancellationToken,
        ServiceJobFormViewModel? formOverride = null)
    {
        var page = await _apiClient.GetAsync<PagedResult<ServiceJobDto>>(BuildPath(filters), cancellationToken);
        var items = page.Items
            .OrderBy(job => job.Name)
            .Select(job => new ServiceJobListItemViewModel
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description,
                Price = job.Price
            })
            .ToList();

        return new ServiceJobsPageViewModel
        {
            Search = filters.Name,
            Flash = TakeFlash(),
            Jobs = items,
            Form = formOverride ?? new ServiceJobFormViewModel()
        };
    }

    private static string BuildPath(CatalogListFiltersViewModel filters)
    {
        return QueryHelpers.AddQueryString("/api/service-jobs", new Dictionary<string, string?>
        {
            ["page"] = "1",
            ["pageSize"] = "100",
            ["name"] = filters.Name
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

        return new FlashMessageViewModel
        {
            Message = message,
            IsError = TempData["FlashIsError"] is bool value && value
        };
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
