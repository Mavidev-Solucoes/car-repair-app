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
public class CatalogUiController : Controller
{
    private readonly IUiApiClient _apiClient;

    public CatalogUiController(IUiApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/ui/catalog")]
    public async Task<IActionResult> Index([Bind(Prefix = "Filters")][FromQuery] CatalogListFiltersViewModel filters, CancellationToken cancellationToken)
    {
        return View(await BuildPageAsync(filters, cancellationToken));
    }

    [HttpGet("/ui/catalog/{id:guid}/details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var item = await _apiClient.GetAsync<ServiceItemDto>($"/api/serviceitems/{id}", cancellationToken);

        return PartialView("_CatalogDetailsModalContent", new CatalogItemDetailsModalViewModel
        {
            Form = new CatalogItemFormViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Stock = item.Stock
            },
            CanDelete = User.IsInRole("Admin")
        });
    }

    [HttpPost("/ui/catalog/save")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind(Prefix = "Form")] CatalogItemFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.Id = null;
            return View("Index", await BuildPageAsync(new CatalogListFiltersViewModel(), cancellationToken, form));
        }

        try
        {
            await _apiClient.PostAsync<object, ServiceItemDto>(
                "/api/serviceitems",
                new { form.Name, form.Description, form.Price, form.Stock },
                cancellationToken);
            SetFlash("Catalog item created.");

            return Redirect("/ui/catalog");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            form.Id = null;
            return View("Index", await BuildPageAsync(new CatalogListFiltersViewModel(), cancellationToken, form));
        }
    }

    [HttpPost("/ui/catalog/{id:guid}/update")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Form")] CatalogItemFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = id;

        if (!ModelState.IsValid)
            return PartialView("_CatalogDetailsModalContent", new CatalogItemDetailsModalViewModel { Form = form, CanDelete = User.IsInRole("Admin") });

        try
        {
            await _apiClient.PutAsync<object, object>(
                $"/api/serviceitems/{id}",
                new { Id = id, form.Name, form.Description, form.Price, form.Stock },
                cancellationToken);
            SetFlash("Catalog item updated.");
            return HtmxRedirect("/ui/catalog");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            return PartialView("_CatalogDetailsModalContent", new CatalogItemDetailsModalViewModel { Form = form, CanDelete = User.IsInRole("Admin") });
        }
    }

    [HttpPost("/ui/catalog/{id:guid}/delete")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteAsync($"/api/serviceitems/{id}", cancellationToken);
            SetFlash("Catalog item deleted.");
            return Redirect("/ui/catalog");
        }
        catch (UiApiException ex)
        {
            SetFlash(ex.Message, isError: true);
            return Redirect("/ui/catalog");
        }
    }

    private async Task<CatalogPageViewModel> BuildPageAsync(CatalogListFiltersViewModel filters, CancellationToken cancellationToken, CatalogItemFormViewModel? formOverride = null)
    {
        var itemPage = await _apiClient.GetAsync<PagedResult<ServiceItemDto>>(BuildCatalogPath(filters), cancellationToken);

        var items = itemPage.Items
            .OrderBy(item => item.Name)
            .Select(item => new CatalogItemListItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Stock = item.Stock
            })
            .ToList();

        return new CatalogPageViewModel
        {
            Flash = TakeFlash(),
            Filters = filters,
            Form = formOverride ?? new CatalogItemFormViewModel(),
            Items = items
        };
    }

    private static string BuildCatalogPath(CatalogListFiltersViewModel filters)
    {
        return QueryHelpers.AddQueryString("/api/serviceitems", new Dictionary<string, string?>
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
