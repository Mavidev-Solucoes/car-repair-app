using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests.Domain;

public class ServiceOrderTests
{
    private static Employee CreateEmployee(UserRole role = UserRole.Admin) =>
        new("Alice", "alice@example.com", "hash", role);

    private static Customer CreateCustomer() =>
        new("John", "52998224725", "john@example.com", "11987654321", "hash");

    private static ServiceOrder CreateOrder(Guid? userId = null)
    {
        var empId = userId ?? Guid.NewGuid();
        return new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), empId);
    }

    private static ServiceOrderItem CreateItem(Guid orderId, Guid? itemId = null) =>
        new(orderId, itemId ?? Guid.NewGuid(), "Part", 100m, 1);

    private static ServiceOrderJob CreateJob(Guid orderId, Guid? jobId = null, Guid? createdBy = null) =>
        new(orderId, jobId ?? Guid.NewGuid(), "Brake Repair", "Fix brakes", 3000m, createdBy);

    [Fact]
    public void Constructor_SetsInitialStatusToReceived()
    {
        var order = CreateOrder();
        Assert.Equal(ServiceStatus.Received, order.Status);
        Assert.Equal(0, order.TotalPrice);
    }

    [Fact]
    public void Constructor_AddsInitialStatusHistory()
    {
        var order = CreateOrder();
        Assert.Single(order.StatusHistory);
        Assert.Equal(ServiceStatus.Received, order.StatusHistory.First().ToStatus);
    }

    [Fact]
    public void AddServiceItem_FromReceived_TransitionsToDiagnosing()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var item = CreateItem(order.Id);

        order.AddServiceItem(item, userId);

        Assert.Equal(ServiceStatus.Diagnosing, order.Status);
        Assert.Contains(item, order.ServiceItems);
    }

    [Fact]
    public void AddServiceItem_RecalculatesTotal()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var item = new ServiceOrderItem(order.Id, Guid.NewGuid(), "Part", 50m, 3);

        order.AddServiceItem(item, userId);

        Assert.Equal(150m, order.TotalPrice);
    }

    [Fact]
    public void AddServiceItem_WrongUser_ThrowsInvalidOperationException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var order = CreateOrder(ownerId);
        var item = CreateItem(order.Id);

        Assert.Throws<InvalidOperationException>(() => order.AddServiceItem(item, otherId));
    }

    [Fact]
    public void RemoveServiceItem_FromDiagnosing_RemovesItem()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var item = CreateItem(order.Id);
        order.AddServiceItem(item, userId);

        order.RemoveServiceItem(item.Id, userId);

        Assert.Empty(order.ServiceItems);
    }

    [Fact]
    public void RemoveServiceItem_ItemNotFound_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var item = CreateItem(order.Id);
        order.AddServiceItem(item, userId);

        Assert.Throws<InvalidOperationException>(() => order.RemoveServiceItem(Guid.NewGuid(), userId));
    }

    [Fact]
    public void AttachServiceJob_FromReceived_TransitionsToDiagnosing()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var job = CreateJob(order.Id, createdBy: userId);

        order.AttachServiceJob(job, userId);

        Assert.Equal(ServiceStatus.Diagnosing, order.Status);
        Assert.Contains(job, order.ServiceJobs);
    }

    [Fact]
    public void RemoveServiceJob_OpenJob_RemovesIt()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var job = CreateJob(order.Id, createdBy: userId);
        order.AttachServiceJob(job, userId);

        order.RemoveServiceJob(job.Id, userId);

        Assert.Empty(order.ServiceJobs);
    }

    [Fact]
    public void RemoveServiceJob_NotDiagnosing_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);

        Assert.Throws<InvalidOperationException>(() => order.RemoveServiceJob(Guid.NewGuid(), userId));
    }

    [Fact]
    public void RequestApproval_NoJobs_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var item = CreateItem(order.Id);
        order.AddServiceItem(item, userId); // Now in Diagnosing

        Assert.Throws<InvalidOperationException>(() => order.RequestApproval(userId));
    }

    [Fact]
    public void RequestApproval_OpenJobs_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var job = CreateJob(order.Id, createdBy: userId);
        order.AttachServiceJob(job, userId);

        Assert.Throws<InvalidOperationException>(() => order.RequestApproval(userId));
    }

    [Fact]
    public void Approve_FromWaitingForApproval_TransitionsToExecuting()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrderInDiagnosingWithAcknowledgedJob(userId);

        order.RequestApproval(userId);
        order.Approve();

        Assert.Equal(ServiceStatus.Executing, order.Status);
    }

    [Fact]
    public void Approve_NotWaitingForApproval_ThrowsInvalidOperationException()
    {
        var order = CreateOrder();
        Assert.Throws<InvalidOperationException>(() => order.Approve());
    }

    [Fact]
    public void TryFinish_AllJobsCompleted_TransitionsToFinished()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrderInExecutingWithCompletedJob(userId);

        var finished = order.TryFinish();

        Assert.True(finished);
        Assert.Equal(ServiceStatus.Finished, order.Status);
    }

    [Fact]
    public void TryFinish_NoJobs_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);
        var setStatus = typeof(ServiceOrder).GetProperty("Status",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        setStatus?.SetValue(order, ServiceStatus.Executing);

        var finished = order.TryFinish();

        Assert.False(finished);
    }

    [Fact]
    public void Deliver_FromFinished_TransitionsToDelivered()
    {
        var userId = Guid.NewGuid();
        var order = CreateFinishedOrder(userId);

        order.Deliver(userId);

        Assert.Equal(ServiceStatus.Delivered, order.Status);
    }

    [Fact]
    public void Deliver_NotFinished_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);

        Assert.Throws<InvalidOperationException>(() => order.Deliver(userId));
    }

    [Fact]
    public void Dispute_FromFinished_TransitionsToDiagnosing()
    {
        var userId = Guid.NewGuid();
        var order = CreateFinishedOrder(userId);

        order.Dispute(userId);

        Assert.Equal(ServiceStatus.Diagnosing, order.Status);
    }

    [Fact]
    public void Dispute_NotFinished_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);

        Assert.Throws<InvalidOperationException>(() => order.Dispute(userId));
    }

    private static ServiceOrder CreateOrderInDiagnosingWithAcknowledgedJob(Guid userId, out Employee mechanic)
    {
        mechanic = CreateEmployee(UserRole.Mechanic);
        var order = CreateOrder(userId);
        var job = CreateJob(order.Id, createdBy: userId);
        order.AttachServiceJob(job, userId);
        job.Acknowledge(mechanic);
        return order;
    }

    private static ServiceOrder CreateOrderInDiagnosingWithAcknowledgedJob(Guid userId)
    {
        return CreateOrderInDiagnosingWithAcknowledgedJob(userId, out _);
    }

    private static ServiceOrder CreateOrderInExecutingWithCompletedJob(Guid userId)
    {
        var order = CreateOrderInDiagnosingWithAcknowledgedJob(userId, out var mechanic);
        order.RequestApproval(userId);
        order.Approve();
        var job = order.ServiceJobs.First();
        job.StartProgress(mechanic.Id);
        job.Complete(mechanic.Id);
        return order;
    }

    private static ServiceOrder CreateFinishedOrder(Guid userId)
    {
        var order = CreateOrderInExecutingWithCompletedJob(userId);
        order.TryFinish();
        return order;
    }
}

public class ServiceOrderJobTests
{
    [Fact]
    public void Constructor_SetsInitialStatusToOpen()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        Assert.Equal(JobStatus.Open, job.Status);
    }

    [Fact]
    public void Acknowledge_FromOpen_TransitionsToAcknowledged()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);

        job.Acknowledge(mechanic);

        Assert.Equal(JobStatus.Acknowledged, job.Status);
        Assert.Equal(mechanic.Id, job.AssignedUserId);
    }

    [Fact]
    public void Acknowledge_NotOpen_ThrowsInvalidOperationException()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);

        Assert.Throws<InvalidOperationException>(() => job.Acknowledge(mechanic));
    }

    [Fact]
    public void StartProgress_FromAcknowledged_TransitionsToInProgress()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);

        job.StartProgress(mechanic.Id);

        Assert.Equal(JobStatus.InProgress, job.Status);
    }

    [Fact]
    public void StartProgress_WrongUser_ThrowsInvalidOperationException()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);

        Assert.Throws<InvalidOperationException>(() => job.StartProgress(Guid.NewGuid()));
    }

    [Fact]
    public void Complete_FromInProgress_TransitionsToCompleted()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);
        job.StartProgress(mechanic.Id);

        job.Complete(mechanic.Id);

        Assert.Equal(JobStatus.Completed, job.Status);
    }

    [Fact]
    public void Complete_NotInProgress_ThrowsInvalidOperationException()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);

        Assert.Throws<InvalidOperationException>(() => job.Complete(mechanic.Id));
    }

    [Fact]
    public void Complete_WrongUser_ThrowsInvalidOperationException()
    {
        var job = new ServiceOrderJob(Guid.NewGuid(), Guid.NewGuid(), "Brake Check", "Check brakes", 500m);
        var mechanic = new Employee("Mech", "m@m.com", "hash", UserRole.Mechanic);
        job.Acknowledge(mechanic);
        job.StartProgress(mechanic.Id);

        Assert.Throws<InvalidOperationException>(() => job.Complete(Guid.NewGuid()));
    }
}

public class ServiceItemTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var item = new ServiceItem("Oil Change", "Change oil", 150m, 10);
        Assert.Equal("Oil Change", item.Name);
        Assert.Equal("Change oil", item.Description);
        Assert.Equal(150m, item.Price);
        Assert.Equal(10, item.Stock);
    }

    [Fact]
    public void ReserveStock_SufficientStock_DecreasesStock()
    {
        var item = new ServiceItem("Oil Change", "Change oil", 150m, 10);
        item.ReserveStock(3);
        Assert.Equal(7, item.Stock);
    }

    [Fact]
    public void ReserveStock_InsufficientStock_ThrowsInvalidOperationException()
    {
        var item = new ServiceItem("Oil Change", "Change oil", 150m, 2);
        Assert.Throws<InvalidOperationException>(() => item.ReserveStock(5));
    }

    [Fact]
    public void ReserveStock_ZeroQuantity_ThrowsInvalidOperationException()
    {
        var item = new ServiceItem("Oil Change", "Change oil", 150m, 10);
        Assert.Throws<InvalidOperationException>(() => item.ReserveStock(0));
    }

    [Fact]
    public void RestoreStock_ValidQuantity_IncreasesStock()
    {
        var item = new ServiceItem("Oil Change", "Change oil", 150m, 5);
        item.RestoreStock(3);
        Assert.Equal(8, item.Stock);
    }

    [Fact]
    public void RestoreStock_ZeroQuantity_ThrowsInvalidOperationException()
    {
        var item = new ServiceItem("Oil Change", "Change oil", 150m, 5);
        Assert.Throws<InvalidOperationException>(() => item.RestoreStock(0));
    }

    [Fact]
    public void Update_UpdatesAllProperties()
    {
        var item = new ServiceItem("Oil Change", "Old desc", 100m, 5);
        item.Update("New Name", "New desc", 200m, 10);
        Assert.Equal("New Name", item.Name);
        Assert.Equal("New desc", item.Description);
        Assert.Equal(200m, item.Price);
        Assert.Equal(10, item.Stock);
    }
}

public class UserTests
{
    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var user = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        user.Deactivate();
        Assert.False(user.IsActive);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        var user = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        user.Deactivate();
        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void UpdatePassword_UpdatesPasswordHash()
    {
        var user = new Employee("Alice", "alice@example.com", "oldhash", UserRole.Admin);
        user.UpdatePassword("newhash");
        Assert.Equal("newhash", user.PasswordHash);
    }

    [Fact]
    public void Employee_CustomerRole_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Employee("Alice", "alice@example.com", "hash", UserRole.Customer));
    }

    [Fact]
    public void Customer_Constructor_SetsCustomerRole()
    {
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        Assert.Equal(UserRole.Customer, customer.Role);
        Assert.True(customer.IsActive);
    }

    [Fact]
    public void Customer_PersonalId_StripsFormatting()
    {
        var customer = new Customer("John", "529.982.247-25", "john@example.com", "11987654321", "hash");
        Assert.Equal("52998224725", customer.PersonalId);
    }
}

public class VehicleTests
{
    [Fact]
    public void Constructor_StripsLicensePlateDashesAndUpperCases()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", 2022, "abc-1d23", "White");
        Assert.Equal("ABC1D23", vehicle.LicensePlate);
    }

    [Fact]
    public void Update_UpdatesAllProperties()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", 2022, "ABC1D23", "White");
        vehicle.Update("Honda", "Civic", 2023, "DEF4G56", "Red");
        Assert.Equal("Honda", vehicle.Brand);
        Assert.Equal("Civic", vehicle.Model);
        Assert.Equal(2023, vehicle.Year);
        Assert.Equal("DEF4G56", vehicle.LicensePlate);
        Assert.Equal("Red", vehicle.Color);
    }
}
