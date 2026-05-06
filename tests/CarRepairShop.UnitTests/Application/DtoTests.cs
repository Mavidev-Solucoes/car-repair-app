using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests.Application;

/// <summary>
/// Exercises constructor, property access, and record equality for all DTO types.
/// </summary>
public class DtoTests
{
    // ── ServiceOrderItemDto ────────────────────────────────────────────────────

    [Fact]
    public void ServiceOrderItemDto_Constructor_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var dto = new ServiceOrderItemDto(id, orderId, itemId, "Oil filter", 29.99m, 2);

        Assert.Equal(id, dto.Id);
        Assert.Equal(orderId, dto.ServiceOrderId);
        Assert.Equal(itemId, dto.ServiceItemId);
        Assert.Equal("Oil filter", dto.Description);
        Assert.Equal(29.99m, dto.Price);
        Assert.Equal(2, dto.Quantity);
    }

    [Fact]
    public void ServiceOrderItemDto_EqualityAndHashCode()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var a = new ServiceOrderItemDto(id, orderId, itemId, "Oil filter", 29.99m, 2);
        var b = new ServiceOrderItemDto(id, orderId, itemId, "Oil filter", 29.99m, 2);
        var c = new ServiceOrderItemDto(Guid.NewGuid(), orderId, itemId, "Oil filter", 29.99m, 2);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a == b);
        Assert.False(a == c);
    }

    [Fact]
    public void ServiceOrderItemDto_WithExpression()
    {
        var dto = new ServiceOrderItemDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "desc", 10m, 1);
        var modified = dto with { Quantity = 5 };

        Assert.Equal(5, modified.Quantity);
        Assert.Equal(dto.Id, modified.Id);
    }

    // ── ServiceOrderJobDto ─────────────────────────────────────────────────────

    [Fact]
    public void ServiceOrderJobDto_Constructor_WithAllOptionals()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var createdUserId = Guid.NewGuid();
        var updatedUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dto = new ServiceOrderJobDto(
            id, orderId, jobId, "Brake Check", "Check brakes", 500m,
            "Open", assignedUserId, now, createdUserId, updatedUserId);

        Assert.Equal(id, dto.Id);
        Assert.Equal(orderId, dto.ServiceOrderId);
        Assert.Equal(jobId, dto.ServiceJobId);
        Assert.Equal("Brake Check", dto.Name);
        Assert.Equal("Check brakes", dto.Description);
        Assert.Equal(500m, dto.Price);
        Assert.Equal("Open", dto.Status);
        Assert.Equal(assignedUserId, dto.AssignedUserId);
        Assert.Equal(now, dto.CreatedAt);
        Assert.Equal(createdUserId, dto.CreatedUserId);
        Assert.Equal(updatedUserId, dto.LastUpdatedUserId);
    }

    [Fact]
    public void ServiceOrderJobDto_Constructor_WithNullOptionals()
    {
        var dto = new ServiceOrderJobDto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Job", "desc", 100m, "Open",
            null, DateTime.UtcNow,
            null, null);

        Assert.Null(dto.AssignedUserId);
        Assert.Null(dto.CreatedUserId);
        Assert.Null(dto.LastUpdatedUserId);
    }

    [Fact]
    public void ServiceOrderJobDto_EqualityAndHashCode()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var a = new ServiceOrderJobDto(id, Guid.NewGuid(), Guid.NewGuid(), "J", "D", 10m, "Open", null, now, null, null);
        var b = new ServiceOrderJobDto(id, a.ServiceOrderId, a.ServiceJobId, "J", "D", 10m, "Open", null, now, null, null);
        var c = new ServiceOrderJobDto(Guid.NewGuid(), a.ServiceOrderId, a.ServiceJobId, "J", "D", 10m, "Open", null, now, null, null);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ── ServiceOrderJobStatusHistoryDto ────────────────────────────────────────

    [Fact]
    public void ServiceOrderJobStatusHistoryDto_Constructor_WithAllFields()
    {
        var id = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var elapsed = TimeSpan.FromMinutes(30);

        var dto = new ServiceOrderJobStatusHistoryDto(
            id, jobId, "Open", "InProgress", now, changedBy, elapsed);

        Assert.Equal(id, dto.Id);
        Assert.Equal(jobId, dto.ServiceOrderJobId);
        Assert.Equal("Open", dto.FromStatus);
        Assert.Equal("InProgress", dto.ToStatus);
        Assert.Equal(now, dto.ChangedAt);
        Assert.Equal(changedBy, dto.ChangedByUserId);
        Assert.Equal(elapsed, dto.TimeInPreviousStatus);
    }

    [Fact]
    public void ServiceOrderJobStatusHistoryDto_Constructor_WithNullOptionals()
    {
        var dto = new ServiceOrderJobStatusHistoryDto(
            Guid.NewGuid(), Guid.NewGuid(),
            null, "Open", DateTime.UtcNow,
            null, null);

        Assert.Null(dto.FromStatus);
        Assert.Null(dto.ChangedByUserId);
        Assert.Null(dto.TimeInPreviousStatus);
    }

    [Fact]
    public void ServiceOrderJobStatusHistoryDto_EqualityAndHashCode()
    {
        var id = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var a = new ServiceOrderJobStatusHistoryDto(id, jobId, null, "Open", now, null, null);
        var b = new ServiceOrderJobStatusHistoryDto(id, jobId, null, "Open", now, null, null);
        var c = new ServiceOrderJobStatusHistoryDto(Guid.NewGuid(), jobId, null, "Open", now, null, null);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ── ServiceStatusHistoryDto ────────────────────────────────────────────────

    [Fact]
    public void ServiceStatusHistoryDto_Constructor_WithAllFields()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dto = new ServiceStatusHistoryDto(
            id, orderId, "Received", "Diagnosing", now, changedBy);

        Assert.Equal(id, dto.Id);
        Assert.Equal(orderId, dto.ServiceOrderId);
        Assert.Equal("Received", dto.FromStatus);
        Assert.Equal("Diagnosing", dto.ToStatus);
        Assert.Equal(now, dto.ChangedAt);
        Assert.Equal(changedBy, dto.ChangedByUserId);
    }

    [Fact]
    public void ServiceStatusHistoryDto_Constructor_WithNullOptionals()
    {
        var dto = new ServiceStatusHistoryDto(
            Guid.NewGuid(), Guid.NewGuid(),
            null, "Received", DateTime.UtcNow,
            null);

        Assert.Null(dto.FromStatus);
        Assert.Null(dto.ChangedByUserId);
    }

    [Fact]
    public void ServiceStatusHistoryDto_EqualityAndHashCode()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var a = new ServiceStatusHistoryDto(id, orderId, null, "Received", now, null);
        var b = new ServiceStatusHistoryDto(id, orderId, null, "Received", now, null);
        var c = new ServiceStatusHistoryDto(Guid.NewGuid(), orderId, null, "Received", now, null);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a == b);
        Assert.False(a == c);
    }

    [Fact]
    public void ServiceStatusHistoryDto_WithExpression()
    {
        var dto = new ServiceStatusHistoryDto(Guid.NewGuid(), Guid.NewGuid(), null, "Received", DateTime.UtcNow, null);
        var modified = dto with { ToStatus = "Diagnosing" };

        Assert.Equal("Diagnosing", modified.ToStatus);
        Assert.Equal(dto.Id, modified.Id);
    }

    // ── CustomerDto ───────────────────────────────────────────────────────────

    [Fact]
    public void CustomerDto_Constructor_WithOptionals()
    {
        var createdBy = Guid.NewGuid();
        var updatedBy = Guid.NewGuid();
        var dto = new CustomerDto(
            Guid.NewGuid(), "John", "52998224725", "john@example.com",
            "11987654321", true, DateTime.UtcNow, createdBy, updatedBy);

        Assert.Equal("John", dto.Name);
        Assert.Equal(createdBy, dto.CreatedUserId);
        Assert.Equal(updatedBy, dto.LastUpdatedUserId);
    }

    [Fact]
    public void CustomerDto_Constructor_WithNullOptionals()
    {
        var dto = new CustomerDto(
            Guid.NewGuid(), "John", "52998224725", "john@example.com",
            "11987654321", true, DateTime.UtcNow);

        Assert.Null(dto.CreatedUserId);
        Assert.Null(dto.LastUpdatedUserId);
    }

    // ── VehicleDto ─────────────────────────────────────────────────────────────

    [Fact]
    public void VehicleDto_Constructor_WithNullColor()
    {
        var dto = new VehicleDto(
            Guid.NewGuid(), Guid.NewGuid(), "Toyota", "Corolla", 2020, "ABC1234", null, DateTime.UtcNow);

        Assert.Null(dto.Color);
        Assert.Equal("Toyota", dto.Brand);
    }

    [Fact]
    public void VehicleDto_Constructor_WithColor()
    {
        var dto = new VehicleDto(
            Guid.NewGuid(), Guid.NewGuid(), "Honda", "Civic", 2021, "ABC1234", "White", DateTime.UtcNow);

        Assert.Equal("White", dto.Color);
    }

    // ── ServiceOrderDto ───────────────────────────────────────────────────────

    [Fact]
    public void ServiceOrderDto_Constructor_WithNestedCollections()
    {
        var orderId = Guid.NewGuid();
        var items = new List<ServiceOrderItemDto>
        {
            new(Guid.NewGuid(), orderId, Guid.NewGuid(), "desc", 10m, 1)
        };
        var jobs = new List<ServiceOrderJobDto>
        {
            new(Guid.NewGuid(), orderId, Guid.NewGuid(), "Job", "desc", 50m, "Open", null, DateTime.UtcNow, null, null)
        };
        var history = new List<ServiceStatusHistoryDto>
        {
            new(Guid.NewGuid(), orderId, null, "Received", DateTime.UtcNow, null)
        };

        var dto = new ServiceOrderDto(
            orderId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Received", 60m, DateTime.UtcNow, items, jobs, history);

        Assert.Single(dto.ServiceItems);
        Assert.Single(dto.ServiceJobs);
        Assert.Single(dto.StatusHistory);
    }

    // ── ServiceItemDto ────────────────────────────────────────────────────────

    [Fact]
    public void ServiceItemDto_Constructor_WithOptionals()
    {
        var createdBy = Guid.NewGuid();
        var dto = new ServiceItemDto(
            Guid.NewGuid(), "Filter", "Air filter", 25m, 10, DateTime.UtcNow, createdBy, null);

        Assert.Equal("Filter", dto.Name);
        Assert.Equal(createdBy, dto.CreatedUserId);
        Assert.Null(dto.LastUpdatedUserId);
    }

    // ── ServiceJobDto ─────────────────────────────────────────────────────────

    [Fact]
    public void ServiceJobDto_Constructor_WithNullOptionals()
    {
        var dto = new ServiceJobDto(
            Guid.NewGuid(), "Brake Check", "desc", 200m, DateTime.UtcNow);

        Assert.Null(dto.CreatedUserId);
        Assert.Null(dto.LastUpdatedUserId);
        Assert.Null(dto.AverageTimeInProgress);
    }

    [Fact]
    public void ServiceJobDto_Constructor_WithAverageTime()
    {
        var avg = TimeSpan.FromHours(2);
        var dto = new ServiceJobDto(
            Guid.NewGuid(), "Brake Check", "desc", 200m, DateTime.UtcNow,
            Guid.NewGuid(), Guid.NewGuid(), avg);

        Assert.Equal(avg, dto.AverageTimeInProgress);
    }
}
