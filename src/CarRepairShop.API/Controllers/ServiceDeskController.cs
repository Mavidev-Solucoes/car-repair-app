using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRepairShop.API.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class ServiceDeskController : Controller
{
    private readonly IUiApiClient _apiClient;

    public ServiceDeskController(IUiApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/ui/services")]
    public async Task<IActionResult> Index([FromQuery] ServicesPageViewModel filters, CancellationToken cancellationToken)
    {
        var model = await BuildServicesPageAsync(filters, cancellationToken);
        return View(model);
    }

    [HttpGet("/ui/services/results")]
    public async Task<IActionResult> Results([FromQuery] ServicesPageViewModel filters, CancellationToken cancellationToken)
    {
        var model = await BuildServicesPageAsync(filters, cancellationToken);
        return PartialView("_ServiceResults", model);
    }

    [HttpGet("/ui/services/new")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!CanCreateServices())
            return Forbid();

        var model = await BuildServiceCreateViewModelAsync(new ServiceCreateViewModel(), cancellationToken);
        return View(model);
    }

    [HttpGet("/ui/services/vehicle-options")]
    public async Task<IActionResult> VehicleOptions([FromQuery] Guid? customerId, [FromQuery] Guid? selectedVehicleId, CancellationToken cancellationToken)
    {
        var options = await GetVehicleOptionsAsync(customerId, cancellationToken);
        ViewData["SelectedVehicleId"] = selectedVehicleId;
        return PartialView("_VehicleOptions", options);
    }

    [HttpPost("/ui/services/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!CanCreateServices())
            return Forbid();

        if (!ModelState.IsValid)
            return View(await BuildServiceCreateViewModelAsync(model, cancellationToken));

        try
        {
            var result = await _apiClient.PostAsync<object, ServiceOrderDto>(
                "/api/services",
                new { VehicleId = model.VehicleId!.Value, CustomerId = model.CustomerId!.Value },
                cancellationToken);
            SetFlash("Service created.");
            return Redirect($"/ui/services/{result.Id}");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex);
            return View(await BuildServiceCreateViewModelAsync(model, cancellationToken));
        }
    }

    [HttpGet("/ui/services/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var model = await BuildServiceDetailAsync(id, cancellationToken);
        return View(model);
    }

    [HttpPost("/ui/services/{id:guid}/items")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(Guid id, [Bind(Prefix = "ItemForm")] AddServiceItemFormViewModel form, CancellationToken cancellationToken)
    {
        if (!CanEditService())
            return Forbid();

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildServiceDetailAsync(id, cancellationToken, itemForm: form);
            return View("Details", invalidModel);
        }

        try
        {
            await _apiClient.PostAsync(
                $"/api/services/{id}/items",
                new { ServiceItemId = form.CatalogItemId!.Value, form.Quantity },
                cancellationToken);
            SetFlash("Item added.");
            return Redirect($"/ui/services/{id}");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "ItemForm");
            var invalidModel = await BuildServiceDetailAsync(id, cancellationToken, itemForm: form);
            return View("Details", invalidModel);
        }
    }

    [HttpPost("/ui/services/{id:guid}/items/{itemId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        if (!CanEditService())
            return Forbid();

        try
        {
            await _apiClient.DeleteAsync($"/api/services/{id}/items/{itemId}", cancellationToken);
            SetFlash("Item removed.");
        }
        catch (UiApiException ex)
        {
            SetFlash(ex.Message, isError: true);
        }

        return Redirect($"/ui/services/{id}");
    }

    [HttpPost("/ui/services/{id:guid}/jobs")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddJob(Guid id, [Bind(Prefix = "JobForm")] AddServiceJobFormViewModel form, CancellationToken cancellationToken)
    {
        if (!CanEditService())
            return Forbid();

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildServiceDetailAsync(id, cancellationToken, jobForm: form);
            return View("Details", invalidModel);
        }

        try
        {
            await _apiClient.PostAsync(
                $"/api/services/{id}/jobs",
                new { ServiceJobId = form.CatalogJobId!.Value },
                cancellationToken);
            SetFlash("Job added.");
            return Redirect($"/ui/services/{id}");
        }
        catch (UiApiException ex)
        {
            AddApiErrors(ex, "JobForm");
            var invalidModel = await BuildServiceDetailAsync(id, cancellationToken, jobForm: form);
            return View("Details", invalidModel);
        }
    }

    [HttpPost("/ui/services/{serviceId:guid}/jobs/{jobId:guid}/delete")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJob(Guid serviceId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!CanEditService())
            return Forbid();

        try
        {
            await _apiClient.DeleteAsync($"/api/services/{serviceId}/jobs/{jobId}", cancellationToken);
            SetFlash("Job removed.");
        }
        catch (UiApiException ex)
        {
            SetFlash(ex.Message, isError: true);
        }

        return Redirect($"/ui/services/{serviceId}");
    }

    [HttpPost("/ui/services/{id:guid}/request-approval")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RequestApproval(Guid id, CancellationToken cancellationToken) =>
        ExecuteDetailActionAsync(id, async () => await _apiClient.PatchAsync($"/api/services/{id}/request-approval", cancellationToken), "Approval request sent.", cancellationToken);

    [HttpPost("/ui/services/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        ExecuteDetailActionAsync(id, async () => await _apiClient.GetAsync<ServiceOrderDto>($"/api/services/{id}/approve", cancellationToken), "Service approved.", cancellationToken);

    [HttpPost("/ui/services/{id:guid}/deliver")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken) =>
        ExecuteDetailActionAsync(id, async () => await _apiClient.PatchAsync($"/api/services/{id}/deliver", cancellationToken), "Service marked delivered.", cancellationToken);

    [HttpPost("/ui/services/{id:guid}/dispute")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Dispute(Guid id, CancellationToken cancellationToken) =>
        ExecuteDetailActionAsync(id, async () => await _apiClient.PatchAsync($"/api/services/{id}/dispute", cancellationToken), "Service reopened for diagnosing.", cancellationToken);

    [HttpPost("/ui/services/{serviceId:guid}/jobs/{jobId:guid}/acknowledge")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AcknowledgeJob(Guid serviceId, Guid jobId, CancellationToken cancellationToken) =>
        ExecuteDetailActionAsync(serviceId, async () => await _apiClient.PatchAsync($"/api/order-jobs/{jobId}/acknowledge", cancellationToken), "Job acknowledged.", cancellationToken);

    [HttpPost("/ui/services/{serviceId:guid}/jobs/{jobId:guid}/start-progress")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> StartJobProgress(Guid serviceId, Guid jobId, CancellationToken cancellationToken) =>
        ExecuteDetailActionAsync(serviceId, async () => await _apiClient.PatchAsync($"/api/order-jobs/{jobId}/start-progress", cancellationToken), "Job moved to in progress.", cancellationToken);

    [HttpPost("/ui/services/{serviceId:guid}/jobs/{jobId:guid}/complete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CompleteJob(Guid serviceId, Guid jobId, CancellationToken cancellationToken) =>
        ExecuteDetailActionAsync(serviceId, async () => await _apiClient.PatchAsync($"/api/order-jobs/{jobId}/complete", cancellationToken), "Job completed.", cancellationToken);

    private async Task<IActionResult> ExecuteDetailActionAsync(
        Guid serviceId,
        Func<Task> action,
        string successMessage,
        CancellationToken cancellationToken)
    {
        FlashMessageViewModel? flash = null;

        try
        {
            await action();
            flash = new FlashMessageViewModel { Message = successMessage };
        }
        catch (UiApiException ex)
        {
            flash = new FlashMessageViewModel { Message = ex.Message, IsError = true };
        }

        var model = await BuildServiceDetailAsync(serviceId, cancellationToken, flash);
        return PartialView("_ServiceDetailContent", model);
    }

    private async Task<ServicesPageViewModel> BuildServicesPageAsync(ServicesPageViewModel filters, CancellationToken cancellationToken)
    {
        var ordersTask = _apiClient.GetAsync<List<ServiceOrderDto>>("/api/services", cancellationToken);
        var customerPageTask = _apiClient.GetAsync<PagedResult<CustomerDto>>("/api/customers?page=1&pageSize=200", cancellationToken);
        var vehiclePageTask = _apiClient.GetAsync<PagedResult<VehicleDto>>("/api/vehicles?page=1&pageSize=200", cancellationToken);

        await Task.WhenAll(ordersTask, customerPageTask, vehiclePageTask);

        var orders = await ordersTask;
        var customers = (await customerPageTask).Items.ToDictionary(customer => customer.Id);
        var vehicles = (await vehiclePageTask).Items.ToDictionary(vehicle => vehicle.Id);

        var allServices = ServiceDeskViewModelFactory.BuildServiceListItems(orders, customers, vehicles, User);
        var services = ServiceDeskViewModelFactory.FilterServices(allServices, filters.Search, filters.Status);
        var statusCounts = ServiceDeskViewModelFactory.BuildStatusCounts(allServices);

        return new ServicesPageViewModel
        {
            Search = filters.Search,
            Status = filters.Status,
            CanCreateService = CanCreateServices(),
            Flash = TakeFlash(),
            StatusCounts = statusCounts,
            Services = services
        };
    }

    private async Task<ServiceCreateViewModel> BuildServiceCreateViewModelAsync(ServiceCreateViewModel model, CancellationToken cancellationToken)
    {
        var customerPage = await _apiClient.GetAsync<PagedResult<CustomerDto>>(
            "/api/customers?page=1&pageSize=200",
            cancellationToken);

        model.Customers = customerPage.Items
            .OrderBy(customer => customer.Name)
            .Select(customer => new LookupOptionViewModel
            {
                Id = customer.Id,
                Label = customer.Name + " · " + customer.Email
            })
            .ToList();

        model.Vehicles = await GetVehicleOptionsAsync(model.CustomerId, cancellationToken);
        model.CanCreate = CanCreateServices();
        model.Flash ??= TakeFlash();
        return model;
    }

    private async Task<IReadOnlyList<LookupOptionViewModel>> GetVehicleOptionsAsync(Guid? customerId, CancellationToken cancellationToken)
    {
        if (!customerId.HasValue)
            return [];

        var vehicles = await _apiClient.GetAsync<List<VehicleDto>>($"/api/vehicles/customer/{customerId.Value}", cancellationToken);
        return vehicles
            .OrderBy(vehicle => vehicle.Brand)
            .ThenBy(vehicle => vehicle.Model)
            .Select(vehicle => new LookupOptionViewModel
            {
                Id = vehicle.Id,
                Label = $"{vehicle.Brand} {vehicle.Model} · {vehicle.LicensePlate}"
            })
            .ToList();
    }

    private async Task<ServiceDetailViewModel> BuildServiceDetailAsync(
        Guid id,
        CancellationToken cancellationToken,
        FlashMessageViewModel? flash = null,
        AddServiceItemFormViewModel? itemForm = null,
        AddServiceJobFormViewModel? jobForm = null)
    {
        var service = await _apiClient.GetAsync<ServiceOrderDto>($"/api/services/{id}", cancellationToken);
        var serviceStatus = Enum.Parse<ServiceStatus>(service.Status, true);

        var customerTask = _apiClient.GetAsync<CustomerDto>($"/api/customers/{service.CustomerId}", cancellationToken);
        var vehicleTask = _apiClient.GetAsync<VehicleDto>($"/api/vehicles/{service.VehicleId}", cancellationToken);
        var catalogTask = _apiClient.GetAsync<PagedResult<ServiceItemDto>>("/api/serviceitems?page=1&pageSize=200", cancellationToken);
        var jobCatalogTask = _apiClient.GetAsync<PagedResult<ServiceJobDto>>("/api/service-jobs?page=1&pageSize=200", cancellationToken);
        var historyTask = _apiClient.GetAsync<List<ServiceStatusHistoryDto>>($"/api/services/{id}/history", cancellationToken);

        await Task.WhenAll(customerTask, vehicleTask, catalogTask, jobCatalogTask, historyTask);

        var customer = await customerTask;
        var vehicle = await vehicleTask;
        var catalogItems = (await catalogTask).Items
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
        var catalogJobs = (await jobCatalogTask).Items
            .OrderBy(job => job.Name)
            .Select(job => new CatalogJobListItemViewModel
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description,
                Price = job.Price
            })
            .ToList();

        var jobs = ServiceDeskViewModelFactory.BuildJobRows(service.ServiceJobs, serviceStatus, User);

        var jobStates = jobs.Select(job => job.Status).ToHashSet();
        var history = await historyTask;
        var canModifyServiceItems = CanModifyServiceItems(serviceStatus);

        return new ServiceDetailViewModel
        {
            Id = service.Id,
            Status = serviceStatus,
            CustomerId = service.CustomerId,
            VehicleId = service.VehicleId,
            CustomerName = customer.Name,
            CustomerEmail = customer.Email,
            CustomerTelephone = customer.Telephone,
            VehicleLabel = vehicle.Brand + " " + vehicle.Model,
            LicensePlate = vehicle.LicensePlate,
            AssignedEmployee = UiDisplayService.FormatUserLabel(service.AssignedUserId, User),
            CreatedAt = service.CreatedAt,
            PartsTotal = service.ServiceItems.Sum(item => item.Price * item.Quantity),
            LaborTotal = service.ServiceJobs.Sum(job => job.Price),
            IsAdmin = User.IsInRole(UserRole.Admin.ToString()),
            CanEditService = CanEditService(),
            CanModifyServiceItems = canModifyServiceItems,
            Flash = flash ?? TakeFlash(),
            ItemForm = itemForm ?? new AddServiceItemFormViewModel(),
            JobForm = jobForm ?? new AddServiceJobFormViewModel(),
            CatalogItems = catalogItems,
            CatalogJobs = catalogJobs,
            Items = service.ServiceItems
                .OrderBy(item => item.Description)
                .Select(item => new ServiceItemRowViewModel
                {
                    Id = item.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Price = item.Price
                })
                .ToList(),
                Jobs = jobs,
                History = ServiceDeskViewModelFactory.BuildHistoryRows(history),
                CanRequestApproval = CanEditService() && serviceStatus == ServiceStatus.Diagnosing && jobs.Count != 0 && !jobStates.Contains(JobStatus.Open),
                CanApprove = User.IsInRole(UserRole.Customer.ToString()) && serviceStatus == ServiceStatus.WaitingForApproval,
                CanDeliver = CanEditService() && serviceStatus == ServiceStatus.Finished,
            CanDispute = User.IsInRole(UserRole.Customer.ToString()) && serviceStatus == ServiceStatus.Finished
        };
    }

    private bool CanCreateServices() =>
        User.IsInRole(UserRole.Admin.ToString());

    private bool CanEditService() =>
        User.IsInRole(UserRole.Admin.ToString());

    private bool CanModifyServiceItems(ServiceStatus status) =>
        CanEditService() && (status == ServiceStatus.Received || status == ServiceStatus.Diagnosing);

    private void AddApiErrors(UiApiException exception, string? prefix = null)
    {
        if (exception.Errors is null || exception.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        foreach (var (key, messages) in exception.Errors)
        {
            var modelKey = string.IsNullOrWhiteSpace(key)
                ? string.Empty
                : string.IsNullOrWhiteSpace(prefix) ? key : $"{prefix}.{key}";
            foreach (var message in messages)
                ModelState.AddModelError(modelKey, message);
        }
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
        return new FlashMessageViewModel
        {
            Message = message,
            IsError = isError
        };
    }
}
