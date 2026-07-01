using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests.Services.API;

public class ServiceDeskViewModelFactoryTests
{
    [Fact]
    public void BuildServiceListItems_AdminUser_MapsAndSortsServices()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var newestOrder = CreateOrder(customerId, vehicleId, assignedUserId, DateTime.UtcNow.AddMinutes(1), ServiceStatus.Executing);
        var olderOrder = CreateOrder(customerId, vehicleId, null, DateTime.UtcNow, ServiceStatus.Received);
        var user = CreateUser(UserRole.Admin, Guid.NewGuid(), "Admin User");

        var services = ServiceDeskViewModelFactory.BuildServiceListItems(
            [olderOrder, newestOrder],
            new Dictionary<Guid, CustomerDto> { [customerId] = new(customerId, "Alice", "12345678901", "alice@mail.com", "11999990000", true, DateTime.UtcNow) },
            new Dictionary<Guid, VehicleDto> { [vehicleId] = new(vehicleId, customerId, "Ford", "Focus", 2022, "ABC1234", "Black", DateTime.UtcNow) },
            user);

        Assert.Equal(2, services.Count);
        Assert.Equal(newestOrder.Id, services[0].Id);
        Assert.Equal("Alice", services[0].CustomerName);
        Assert.Equal("Ford Focus", services[0].VehicleLabel);
        Assert.Equal("ABC1234", services[0].LicensePlate);
        Assert.Equal(ServiceStatus.Executing, services[0].Status);
        Assert.Equal(80m, services[0].PartsTotal);
        Assert.Equal(120m, services[0].LaborTotal);
    }

    [Fact]
    public void BuildServiceListItems_CustomerUser_ReturnsOnlyOwnServices()
    {
        var currentUserId = Guid.NewGuid();
        var ownOrder = CreateOrder(currentUserId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, ServiceStatus.Received);
        var otherOrder = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1), ServiceStatus.Received);
        var user = CreateUser(UserRole.Customer, currentUserId, "Alice");

        var services = ServiceDeskViewModelFactory.BuildServiceListItems(
            [ownOrder, otherOrder],
            new Dictionary<Guid, CustomerDto>(),
            new Dictionary<Guid, VehicleDto>(),
            user);

        var service = Assert.Single(services);
        Assert.Equal(ownOrder.Id, service.Id);
    }

    [Fact]
    public void FilterServices_AppliesSearchAndStatus()
    {
        var services = new List<ServiceListItemViewModel>
        {
            new() { CustomerName = "Alice", VehicleLabel = "Ford Focus", LicensePlate = "ABC1234", Status = ServiceStatus.Received },
            new() { CustomerName = "Bob", VehicleLabel = "Civic", LicensePlate = "XYZ0000", Status = ServiceStatus.Diagnosing }
        };

        var filtered = ServiceDeskViewModelFactory.FilterServices(services, "Alice", ServiceStatus.Received);

        var service = Assert.Single(filtered);
        Assert.Equal("Alice", service.CustomerName);
    }

    [Fact]
    public void BuildStatusCounts_AlwaysIncludesAllStatusesAndAllBucket()
    {
        var allServices = new List<ServiceListItemViewModel>
        {
            new() { Status = ServiceStatus.Received },
            new() { Status = ServiceStatus.Received },
            new() { Status = ServiceStatus.Diagnosing }
        };

        var counts = ServiceDeskViewModelFactory.BuildStatusCounts(allServices);

        Assert.Equal(Enum.GetValues<ServiceStatus>().Length + 1, counts.Count);
        Assert.Equal(3, counts.First(c => c.Status is null).Count);
        Assert.Equal(2, counts.First(c => c.Status == ServiceStatus.Received).Count);
        Assert.Equal(1, counts.First(c => c.Status == ServiceStatus.Diagnosing).Count);
    }

    [Fact]
    public void BuildJobRows_ConfiguresActionFlagsFromServiceAndJobStatus()
    {
        var user = CreateUser(UserRole.Admin, Guid.NewGuid(), "Admin User");
        var openJob = CreateJob("Open Job", JobStatus.Open);
        var acknowledgedJob = CreateJob("Ack Job", JobStatus.Acknowledged);
        var inProgressJob = CreateJob("In Progress Job", JobStatus.InProgress);

        var diagnosingRows = ServiceDeskViewModelFactory.BuildJobRows([openJob], ServiceStatus.Diagnosing, user);
        var executingRows = ServiceDeskViewModelFactory.BuildJobRows([acknowledgedJob, inProgressJob], ServiceStatus.Executing, user);

        Assert.True(diagnosingRows[0].CanAcknowledge);
        Assert.True(diagnosingRows[0].CanDelete);
        Assert.True(executingRows.Single(r => r.Name == "Ack Job").CanStartProgress);
        Assert.True(executingRows.Single(r => r.Name == "In Progress Job").CanComplete);
    }

    [Fact]
    public void BuildHistoryRows_SortsDescendingAndFormatsLabels()
    {
        var newer = DateTime.UtcNow;
        var older = newer.AddMinutes(-30);
        var history = new List<ServiceStatusHistoryDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Received", "Diagnosing", older, Guid.NewGuid()),
            new(Guid.NewGuid(), Guid.NewGuid(), "Diagnosing", "Executing", newer, Guid.NewGuid())
        };

        var rows = ServiceDeskViewModelFactory.BuildHistoryRows(history);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Diagnosing -> Executing", rows[0].Label);
        Assert.Equal(newer.ToLocalTime().ToString("dd MMM yyyy, HH:mm"), rows[0].Timestamp);
    }

    private static ServiceOrderDto CreateOrder(
        Guid customerId,
        Guid vehicleId,
        Guid? assignedUserId,
        DateTime createdAt,
        ServiceStatus status)
    {
        var serviceOrderId = Guid.NewGuid();
        return new(
            serviceOrderId,
            vehicleId,
            customerId,
            assignedUserId ?? Guid.NewGuid(),
            status.ToString(),
            0m,
            createdAt,
            [new ServiceOrderItemDto(Guid.NewGuid(), serviceOrderId, Guid.NewGuid(), "Oil", 40m, 2)],
            [CreateJob("Inspection", JobStatus.Open, serviceOrderId)],
            []);
    }

    private static ServiceOrderJobDto CreateJob(string name, JobStatus status, Guid? serviceOrderId = null) =>
        new(Guid.NewGuid(), serviceOrderId ?? Guid.NewGuid(), Guid.NewGuid(), name, "Desc", 120m, status.ToString(), Guid.NewGuid(), DateTime.UtcNow);

    private static ClaimsPrincipal CreateUser(UserRole role, Guid userId, string name)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.Name, name)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
