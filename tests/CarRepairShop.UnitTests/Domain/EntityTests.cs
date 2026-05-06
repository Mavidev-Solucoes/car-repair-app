using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests.Domain;

public class EmployeeTests
{
    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Mechanic)]
    [InlineData(UserRole.Receptionist)]
    public void Constructor_WithValidEmployeeRole_CreatesEmployee(UserRole role)
    {
        var employee = new Employee("Alice", "alice@shop.com", "hash", role);

        Assert.Equal("Alice", employee.Name);
        Assert.Equal(role, employee.Role);
    }

    [Fact]
    public void Constructor_WithCustomerRole_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Employee("Alice", "alice@shop.com", "hash", UserRole.Customer));
    }

    [Fact]
    public void Update_WithValidRole_UpdatesEmployee()
    {
        var employee = new Employee("Alice", "alice@shop.com", "hash", UserRole.Admin);

        employee.Update("Bob", "bob@shop.com", UserRole.Mechanic);

        Assert.Equal("Bob", employee.Name);
        Assert.Equal("bob@shop.com", employee.Email);
        Assert.Equal(UserRole.Mechanic, employee.Role);
    }

    [Fact]
    public void Update_WithCustomerRole_ThrowsInvalidOperationException()
    {
        var employee = new Employee("Alice", "alice@shop.com", "hash", UserRole.Admin);

        Assert.Throws<InvalidOperationException>(() =>
            employee.Update("Alice", "alice@shop.com", UserRole.Customer));
    }
}

public class ServiceOrderJobEntityTests
{
    [Fact]
    public void Constructor_SetsPropertiesAndInitialHistory()
    {
        var serviceOrderId = Guid.NewGuid();
        var serviceJobId = Guid.NewGuid();
        var job = new ServiceOrderJob(serviceOrderId, serviceJobId, "Brake Check", "Check brakes", 300m);

        Assert.Equal(serviceOrderId, job.ServiceOrderId);
        Assert.Equal(serviceJobId, job.ServiceJobId);
        Assert.Equal("Brake Check", job.Name);
        Assert.Equal(300m, job.Price);
        Assert.Equal(JobStatus.Open, job.Status);
        Assert.Single(job.StatusHistory);
    }

    [Fact]
    public void Acknowledge_WhenOpen_TransitionsToAcknowledged()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "desc", 300m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);

        job.Acknowledge(mechanic);

        Assert.Equal(JobStatus.Acknowledged, job.Status);
        Assert.Equal(mechanic.Id, job.AssignedUserId);
        Assert.Equal(2, job.StatusHistory.Count);
    }

    [Fact]
    public void Acknowledge_WhenNotOpen_Throws()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "desc", 300m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);

        Assert.Throws<InvalidOperationException>(() => job.Acknowledge(mechanic));
    }

    [Fact]
    public void StartProgress_WhenNotAssignedUser_Throws()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "desc", 300m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);

        Assert.Throws<InvalidOperationException>(() => job.StartProgress(Guid.NewGuid()));
    }

    [Fact]
    public void StartProgress_WhenNotAcknowledged_Throws()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "desc", 300m);

        Assert.Throws<InvalidOperationException>(() => job.StartProgress(Guid.NewGuid()));
    }

    [Fact]
    public void Complete_WhenNotInProgress_Throws()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "desc", 300m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);

        Assert.Throws<InvalidOperationException>(() => job.Complete(mechanic.Id));
    }

    [Fact]
    public void Complete_WhenNotAssignedUser_Throws()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "desc", 300m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);
        job.StartProgress(mechanic.Id);

        Assert.Throws<InvalidOperationException>(() => job.Complete(Guid.NewGuid()));
    }

    [Fact]
    public void FullLifecycle_OpenAcknowledgeStartComplete()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "desc", 300m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);

        job.Acknowledge(mechanic);
        job.StartProgress(mechanic.Id);
        job.Complete(mechanic.Id);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(4, job.StatusHistory.Count);
    }
}

public class ServiceItemEntityTests
{
    [Fact]
    public void ReserveStock_WithInsufficientStock_Throws()
    {
        var item = new ServiceItem("Oil Filter", "Oil filter", 50m, 2);

        Assert.Throws<InvalidOperationException>(() => item.ReserveStock(5));
    }

    [Fact]
    public void ReserveStock_WithZeroQuantity_Throws()
    {
        var item = new ServiceItem("Oil Filter", "Oil filter", 50m, 10);

        Assert.Throws<InvalidOperationException>(() => item.ReserveStock(0));
    }

    [Fact]
    public void RestoreStock_WithZeroQuantity_Throws()
    {
        var item = new ServiceItem("Oil Filter", "Oil filter", 50m, 10);

        Assert.Throws<InvalidOperationException>(() => item.RestoreStock(0));
    }
}
