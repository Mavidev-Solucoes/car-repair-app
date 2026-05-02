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
public class OrderJobsUiController : Controller
{
    private readonly IUiApiClient _apiClient;

    public OrderJobsUiController(IUiApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/ui/order-jobs")]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] JobStatus? status, CancellationToken cancellationToken)
    {
        var query = new List<string> { "page=1", "pageSize=100" };
        if (!string.IsNullOrWhiteSpace(search))
            query.Add("name=" + Uri.EscapeDataString(search));
        if (status.HasValue)
            query.Add("status=" + Uri.EscapeDataString(status.Value.ToString()));

        var page = await _apiClient.GetAsync<PagedResult<ServiceOrderJobDto>>("/api/order-jobs?" + string.Join("&", query), cancellationToken);
        var items = page.Items
            .OrderBy(job => job.Name)
            .Select(job => new ServiceJobListItemViewModel
            {
                Id = job.Id,
                ServiceOrderId = job.ServiceOrderId,
                Name = job.Name,
                Description = job.Description,
                Status = Enum.Parse<JobStatus>(job.Status, true),
                Price = job.Price,
                AssignedEmployee = UiDisplayService.FormatUserLabel(job.AssignedUserId, User)
            })
            .ToList();

        return View(new ServiceJobsPageViewModel
        {
            Search = search,
            Status = status,
            Jobs = items
        });
    }

    [HttpGet("/ui/order-jobs/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var job = await _apiClient.GetAsync<ServiceOrderJobDto>($"/api/order-jobs/{id}", cancellationToken);
        var history = await _apiClient.GetAsync<List<ServiceOrderJobStatusHistoryDto>>($"/api/order-jobs/{id}/history", cancellationToken);

        var jobStatus = Enum.Parse<JobStatus>(job.Status, true);

        return View(new ServiceJobDetailViewModel
        {
            Id = job.Id,
            ServiceOrderId = job.ServiceOrderId,
            AssignedEmployee = UiDisplayService.FormatUserLabel(job.AssignedUserId, User),
            Status = jobStatus,
            Form = new ServiceJobFormViewModel
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description,
                Price = job.Price
            },
            History = history
                .OrderByDescending(entry => entry.ChangedAt)
                .Select(entry => new ServiceHistoryRowViewModel
                {
                    Label = string.IsNullOrWhiteSpace(entry.FromStatus) ? entry.ToStatus : entry.FromStatus + " -> " + entry.ToStatus,
                    Timestamp = entry.ChangedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm")
                })
                .ToList(),
            CanAcknowledge = jobStatus == JobStatus.Open,
            CanStartProgress = jobStatus == JobStatus.Acknowledged,
            CanComplete = jobStatus == JobStatus.InProgress
        });
    }

    [HttpPost("/ui/order-jobs/{id:guid}/acknowledge")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        await _apiClient.PatchAsync($"/api/order-jobs/{id}/acknowledge", cancellationToken);
        return Redirect($"/ui/order-jobs/{id}");
    }

    [HttpPost("/ui/order-jobs/{id:guid}/start-progress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProgress(Guid id, CancellationToken cancellationToken)
    {
        await _apiClient.PatchAsync($"/api/order-jobs/{id}/start-progress", cancellationToken);
        return Redirect($"/ui/order-jobs/{id}");
    }

    [HttpPost("/ui/order-jobs/{id:guid}/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await _apiClient.PatchAsync($"/api/order-jobs/{id}/complete", cancellationToken);
        return Redirect($"/ui/order-jobs/{id}");
    }
}
