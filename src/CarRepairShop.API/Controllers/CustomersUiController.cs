using CarRepairShop.API.ViewModels;
using CarRepairShop.API.Services;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace CarRepairShop.API.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class CustomersUiController : Controller
{
    private readonly IUiApiClient _apiClient;

    public CustomersUiController(IUiApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/ui/customers")]
    public async Task<IActionResult> Index([Bind(Prefix = "Filters")][FromQuery] CustomerListFiltersViewModel filters, CancellationToken cancellationToken)
    {
        return View(await BuildPageAsync(filters, cancellationToken));
    }

    [HttpGet("/ui/customers/{id:guid}/details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var customerTask = _apiClient.GetAsync<CustomerDto>($"/api/customers/{id}", cancellationToken);
        var vehiclesTask = _apiClient.GetAsync<List<VehicleDto>>($"/api/vehicles/customer/{id}", cancellationToken);

        await Task.WhenAll(customerTask, vehiclesTask);

        var customer = await customerTask;
        var vehicles = await vehiclesTask;

        var model = BuildModalViewModel(customer, vehicles);

        return PartialView("_CustomerDetailsModalContent", model);
    }

    [HttpPost("/ui/customers/save")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind(Prefix = "Form")] CustomerFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.Id = null;
            return View("Index", await BuildPageAsync(new CustomerListFiltersViewModel(), cancellationToken, form));
        }

        try
        {
            await _apiClient.PostAsync<object, CustomerDto>(
                "/api/customers",
                new { form.Name, form.PersonalId, form.Email, form.Telephone },
                cancellationToken);
            SetFlash("Customer created.");

            return Redirect("/ui/customers");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            form.Id = null;
            return View("Index", await BuildPageAsync(new CustomerListFiltersViewModel(), cancellationToken, form));
        }
    }

    [HttpPost("/ui/customers/{id:guid}/update")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Form")] CustomerFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = id;

        if (!ModelState.IsValid)
        {
            var vehicles = await _apiClient.GetAsync<List<VehicleDto>>($"/api/vehicles/customer/{id}", cancellationToken);
            return PartialView("_CustomerDetailsModalContent", new CustomerDetailsModalViewModel
            {
                Form = form,
                Vehicles = MapVehicles(vehicles),
                CanDelete = User.IsInRole("Admin")
            });
        }

        try
        {
            await _apiClient.PutAsync<object, object>(
                $"/api/customers/{id}",
                new { Id = id, form.Name, form.Email, form.Telephone },
                cancellationToken);
            SetFlash("Customer updated.");
            return HtmxRedirect("/ui/customers");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "Form");
            var vehicles = await _apiClient.GetAsync<List<VehicleDto>>($"/api/vehicles/customer/{id}", cancellationToken);
            return PartialView("_CustomerDetailsModalContent", new CustomerDetailsModalViewModel
            {
                Form = form,
                Vehicles = MapVehicles(vehicles),
                CanDelete = User.IsInRole("Admin")
            });
        }
    }

    [HttpPost("/ui/customers/{id:guid}/delete")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteAsync($"/api/customers/{id}", cancellationToken);
            SetFlash("Customer deleted.");
            return Redirect("/ui/customers");
        }
        catch (UiApiException ex)
        {
            SetFlash(ex.Message, isError: true);
            return Redirect("/ui/customers");
        }
    }

    private async Task<CustomersPageViewModel> BuildPageAsync(CustomerListFiltersViewModel filters, CancellationToken cancellationToken, CustomerFormViewModel? formOverride = null)
    {
        var customerPage = await _apiClient.GetAsync<PagedResult<CustomerDto>>(BuildCustomersPath(filters), cancellationToken);

        var customers = customerPage.Items
            .OrderBy(customer => customer.Name)
            .Select(customer => new CustomerListItemViewModel
            {
                Id = customer.Id,
                Name = customer.Name,
                PersonalId = customer.PersonalId,
                Email = customer.Email,
                Telephone = customer.Telephone
            })
            .ToList();

        return new CustomersPageViewModel
        {
            Flash = TakeFlash(),
            Filters = filters,
            Form = formOverride ?? new CustomerFormViewModel(),
            Customers = customers
        };
    }

    private CustomerDetailsModalViewModel BuildModalViewModel(CustomerDto customer, IReadOnlyList<VehicleDto> vehicles)
    {
        return new CustomerDetailsModalViewModel
        {
            Form = new CustomerFormViewModel
            {
                Id = customer.Id,
                Name = customer.Name,
                PersonalId = customer.PersonalId,
                Email = customer.Email,
                Telephone = customer.Telephone
            },
            Vehicles = MapVehicles(vehicles),
            CanDelete = User.IsInRole("Admin")
        };
    }

    private static IReadOnlyList<VehicleListItemViewModel> MapVehicles(IEnumerable<VehicleDto> vehicles)
    {
        return vehicles
            .OrderBy(vehicle => vehicle.Brand)
            .ThenBy(vehicle => vehicle.Model)
            .Select(vehicle => new VehicleListItemViewModel
            {
                Id = vehicle.Id,
                CustomerId = vehicle.CustomerId,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color
            })
            .ToList();
    }

    private static string BuildCustomersPath(CustomerListFiltersViewModel filters)
    {
        return QueryHelpers.AddQueryString("/api/customers", new Dictionary<string, string?>
        {
            ["page"] = "1",
            ["pageSize"] = "100",
            ["name"] = filters.Name,
            ["email"] = filters.Email,
            ["personalId"] = filters.PersonalId,
            ["telephone"] = filters.Telephone
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
