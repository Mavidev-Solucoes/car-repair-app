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
public class VehiclesUiController : Controller
{
    private readonly IUiApiClient _apiClient;

    public VehiclesUiController(IUiApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/ui/vehicles")]
    public async Task<IActionResult> Index([Bind(Prefix = "Filters")][FromQuery] VehicleListFiltersViewModel filters, CancellationToken cancellationToken)
    {
        return View(await BuildPageAsync(filters, cancellationToken));
    }

    [HttpGet("/ui/vehicles/{id:guid}/details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var vehicleTask = _apiClient.GetAsync<VehicleDto>($"/api/vehicles/{id}", cancellationToken);
        var customerPageTask = _apiClient.GetAsync<PagedResult<CustomerDto>>("/api/customers?page=1&pageSize=200", cancellationToken);

        await Task.WhenAll(vehicleTask, customerPageTask);

        var vehicle = await vehicleTask;
        var customerPage = await customerPageTask;
        var customerName = customerPage.Items.FirstOrDefault(customer => customer.Id == vehicle.CustomerId)?.Name
            ?? vehicle.CustomerId.ToString("N")[..8];

        return PartialView("_VehicleDetailsModalContent", new VehicleDetailsModalViewModel
        {
            Form = new VehicleFormViewModel
            {
                Id = vehicle.Id,
                CustomerId = vehicle.CustomerId,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color
            },
            CustomerName = customerName,
            CanDelete = User.IsInRole("Admin")
        });
    }

    [HttpPost("/ui/vehicles/save")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind(Prefix = "Form")] VehicleFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.Id = null;
            return View("Index", await BuildPageAsync(new VehicleListFiltersViewModel(), cancellationToken, form));
        }

        try
        {
            await _apiClient.PostAsync<object, VehicleDto>(
                "/api/vehicles",
                new { CustomerId = form.CustomerId!.Value, form.Brand, form.Model, form.Year, form.LicensePlate, form.Color },
                cancellationToken);
            SetFlash("Vehicle created.");

            return Redirect("/ui/vehicles");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            form.Id = null;
            return View("Index", await BuildPageAsync(new VehicleListFiltersViewModel(), cancellationToken, form));
        }
    }

    [HttpPost("/ui/vehicles/{id:guid}/update")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Form")] VehicleFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = id;

        var customerPage = await _apiClient.GetAsync<PagedResult<CustomerDto>>("/api/customers?page=1&pageSize=200", cancellationToken);
        var customerName = form.CustomerId.HasValue
            ? customerPage.Items.FirstOrDefault(customer => customer.Id == form.CustomerId.Value)?.Name ?? form.CustomerId.Value.ToString("N")[..8]
            : string.Empty;

        if (!ModelState.IsValid)
        {
            return PartialView("_VehicleDetailsModalContent", new VehicleDetailsModalViewModel
            {
                Form = form,
                CustomerName = customerName,
                CanDelete = User.IsInRole("Admin")
            });
        }

        try
        {
            await _apiClient.PutAsync<object, object>(
                $"/api/vehicles/{id}",
                new { Id = id, form.Brand, form.Model, form.Year, form.LicensePlate, form.Color },
                cancellationToken);
            SetFlash("Vehicle updated.");
            return HtmxRedirect("/ui/vehicles");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            return PartialView("_VehicleDetailsModalContent", new VehicleDetailsModalViewModel
            {
                Form = form,
                CustomerName = customerName,
                CanDelete = User.IsInRole("Admin")
            });
        }
    }

    [HttpPost("/ui/vehicles/{id:guid}/delete")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteAsync($"/api/vehicles/{id}", cancellationToken);
            SetFlash("Vehicle deleted.");
            return Redirect("/ui/vehicles");
        }
        catch (UiApiException ex)
        {
            SetFlash(ex.Message, isError: true);
            return Redirect("/ui/vehicles");
        }
    }

    private async Task<VehiclesPageViewModel> BuildPageAsync(VehicleListFiltersViewModel filters, CancellationToken cancellationToken, VehicleFormViewModel? formOverride = null)
    {
        var vehiclePageTask = _apiClient.GetAsync<PagedResult<VehicleDto>>(
            BuildVehiclesPath(filters),
            cancellationToken);
        var customerPageTask = _apiClient.GetAsync<PagedResult<CustomerDto>>(
            "/api/customers?page=1&pageSize=100",
            cancellationToken);

        await Task.WhenAll(vehiclePageTask, customerPageTask);

        var vehiclePage = await vehiclePageTask;
        var customerPage = await customerPageTask;
        var customerMap = customerPage.Items.ToDictionary(customer => customer.Id, customer => customer.Name);

        var vehicles = vehiclePage.Items
            .OrderBy(vehicle => vehicle.Brand)
            .ThenBy(vehicle => vehicle.Model)
            .Select(vehicle => new VehicleListItemViewModel
            {
                Id = vehicle.Id,
                CustomerId = vehicle.CustomerId,
                CustomerName = customerMap.TryGetValue(vehicle.CustomerId, out var customerName) ? customerName : vehicle.CustomerId.ToString("N")[..8],
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color
            })
            .ToList();

        var customers = customerPage.Items
            .OrderBy(customer => customer.Name)
            .Select(customer => new LookupOptionViewModel
            {
                Id = customer.Id,
                Label = customer.Name
            })
            .ToList();

        return new VehiclesPageViewModel
        {
            Flash = TakeFlash(),
            Filters = filters,
            Form = formOverride ?? new VehicleFormViewModel(),
            Customers = customers,
            Vehicles = vehicles
        };
    }

    private static string BuildVehiclesPath(VehicleListFiltersViewModel filters)
    {
        return QueryHelpers.AddQueryString("/api/vehicles", new Dictionary<string, string?>
        {
            ["page"] = "1",
            ["pageSize"] = "100",
            ["brand"] = filters.Brand,
            ["model"] = filters.Model,
            ["year"] = filters.Year?.ToString(),
            ["licensePlate"] = filters.LicensePlate
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
