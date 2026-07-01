using System.Security.Claims;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.API.Services;

public static class ServiceDeskViewModelFactory
{
    public static List<ServiceListItemViewModel> BuildServiceListItems(
        IReadOnlyList<ServiceOrderDto> orders,
        IReadOnlyDictionary<Guid, CustomerDto> customers,
        IReadOnlyDictionary<Guid, VehicleDto> vehicles,
        ClaimsPrincipal user)
    {
        var allServices = orders
            .Select(order => BuildServiceListItem(order, customers, vehicles, user))
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

        if (!user.IsInRole(UserRole.Customer.ToString()))
            return allServices;

        var currentUserId = UiDisplayService.GetCurrentUserId(user);
        return allServices
            .Where(service => orders.First(order => order.Id == service.Id).CustomerId == currentUserId)
            .ToList();
    }

    public static List<ServiceListItemViewModel> FilterServices(
        IEnumerable<ServiceListItemViewModel> services,
        string? search,
        ServiceStatus? status)
    {
        var filtered = services;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(order =>
                order.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                order.VehicleLabel.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                order.LicensePlate.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
            filtered = filtered.Where(order => order.Status == status.Value);

        return filtered.ToList();
    }

    public static IReadOnlyList<ServiceStatusCountViewModel> BuildStatusCounts(
        IReadOnlyCollection<ServiceListItemViewModel> allServices)
    {
        var statusCounts = allServices
            .GroupBy(service => service.Status)
            .ToDictionary(group => group.Key, group => group.Count());

        return Enum.GetValues<ServiceStatus>()
            .Select(status => new ServiceStatusCountViewModel
            {
                Status = status,
                Label = status.ToString(),
                Count = statusCounts.TryGetValue(status, out var count) ? count : 0
            })
            .Prepend(new ServiceStatusCountViewModel
            {
                Status = null,
                Label = "All",
                Count = allServices.Count
            })
            .ToList();
    }

    public static List<ServiceJobRowViewModel> BuildJobRows(
        IEnumerable<ServiceOrderJobDto> jobs,
        ServiceStatus serviceStatus,
        ClaimsPrincipal user)
    {
        return jobs
            .Select(job =>
            {
                var jobStatus = Enum.Parse<JobStatus>(job.Status, true);
                return new ServiceJobRowViewModel
                {
                    Id = job.Id,
                    ServiceOrderId = job.ServiceOrderId,
                    CatalogJobId = job.ServiceJobId,
                    Name = job.Name,
                    Description = job.Description,
                    Status = jobStatus,
                    Price = job.Price,
                    AssignedEmployee = UiDisplayService.FormatUserLabel(job.AssignedUserId, user),
                    CanAcknowledge = serviceStatus == ServiceStatus.Diagnosing && jobStatus == JobStatus.Open,
                    CanStartProgress = serviceStatus == ServiceStatus.Executing && jobStatus == JobStatus.Acknowledged,
                    CanComplete = serviceStatus == ServiceStatus.Executing && jobStatus == JobStatus.InProgress,
                    CanDelete = user.IsInRole(UserRole.Admin.ToString()) && jobStatus == JobStatus.Open && serviceStatus == ServiceStatus.Diagnosing
                };
            })
            .OrderBy(job => job.Name)
            .ToList();
    }

    public static List<ServiceHistoryRowViewModel> BuildHistoryRows(IEnumerable<ServiceStatusHistoryDto> history)
    {
        return history
            .OrderByDescending(entry => entry.ChangedAt)
            .Select(entry => new ServiceHistoryRowViewModel
            {
                Label = string.IsNullOrWhiteSpace(entry.FromStatus) ? entry.ToStatus : entry.FromStatus + " -> " + entry.ToStatus,
                Timestamp = entry.ChangedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm")
            })
            .ToList();
    }

    private static ServiceListItemViewModel BuildServiceListItem(
        ServiceOrderDto order,
        IReadOnlyDictionary<Guid, CustomerDto> customers,
        IReadOnlyDictionary<Guid, VehicleDto> vehicles,
        ClaimsPrincipal user)
    {
        var status = Enum.Parse<ServiceStatus>(order.Status, true);
        var customerName = customers.TryGetValue(order.CustomerId, out var customer) ? customer.Name : order.CustomerId.ToString("N")[..8];
        var vehicleLabel = vehicles.TryGetValue(order.VehicleId, out var vehicle)
            ? $"{vehicle.Brand} {vehicle.Model}"
            : order.VehicleId.ToString("N")[..8];
        var licensePlate = vehicles.TryGetValue(order.VehicleId, out vehicle) ? vehicle.LicensePlate : string.Empty;

        return new ServiceListItemViewModel
        {
            Id = order.Id,
            CustomerName = customerName,
            VehicleLabel = vehicleLabel,
            LicensePlate = licensePlate,
            AssignedEmployee = UiDisplayService.FormatUserLabel(order.AssignedUserId, user),
            Status = status,
            PartsTotal = order.ServiceItems.Sum(item => item.Price * item.Quantity),
            LaborTotal = order.ServiceJobs.Sum(job => job.Price),
            JobCount = order.ServiceJobs.Count(),
            CreatedAt = order.CreatedAt
        };
    }
}
