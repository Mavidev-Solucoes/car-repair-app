using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests;

public class ServiceOrderTests
{
    private static ServiceOrder CreateOrder(out Guid vehicleId, out Guid customerId, out Guid employeeId)
    {
        vehicleId = Guid.NewGuid();
        customerId = Guid.NewGuid();
        employeeId = Guid.NewGuid();
        return new ServiceOrder(vehicleId, customerId, employeeId);
    }

    private static ServiceOrderItem MakeItem(Guid serviceOrderId, decimal price = 100m, int quantity = 2) =>
        new(serviceOrderId, Guid.NewGuid(), "Oil change", price, quantity);

    private static ServiceOrderJob MakeAcknowledgedJob(Guid serviceOrderId)
    {
        var job = new ServiceOrderJob(serviceOrderId, Guid.NewGuid(), "Brake inspection", "Check brake pads", 50m);
        var employee = new Employee("Tech", "tech@shop.com", "hash", UserRole.Mechanic);
        job.Acknowledge(employee);
        return job;
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var order = CreateOrder(out var vehicleId, out var customerId, out var employeeId);

        Assert.Equal(vehicleId, order.VehicleId);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(employeeId, order.AssignedUserId);
        Assert.Equal(ServiceStatus.Received, order.Status);
        Assert.Equal(0m, order.TotalPrice);
        Assert.Equal(employeeId, order.CreatedUserId);
    }

    [Fact]
    public void AddServiceItem_AddsItemUpdatesTotalAndTransitionsToDiagnosing()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        order.AddServiceItem(MakeItem(order.Id, 50m, 3), employeeId);

        Assert.Single(order.ServiceItems);
        Assert.Equal(150m, order.TotalPrice);
        Assert.Equal(ServiceStatus.Diagnosing, order.Status);
    }

    [Fact]
    public void AddServiceItem_WrongUser_Throws()
    {
        var order = CreateOrder(out _, out _, out _);

        var ex = Assert.Throws<InvalidOperationException>(() => order.AddServiceItem(MakeItem(order.Id), Guid.NewGuid()));

        Assert.Contains("Only the assigned employee", ex.Message);
    }

    [Fact]
    public void RemoveServiceItem_RemovesItemAndUpdatesTotal()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var item = MakeItem(order.Id, 100m, 2);
        order.AddServiceItem(item, employeeId);

        order.RemoveServiceItem(item.Id, employeeId);

        Assert.Empty(order.ServiceItems);
        Assert.Equal(0m, order.TotalPrice);
    }

    [Fact]
    public void AttachServiceJob_AddsJobUpdatesTotalAndTransitionsToDiagnosing()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Tire rotation", "Rotate all four tires", 30m);

        order.AttachServiceJob(job, employeeId);

        Assert.Single(order.ServiceJobs);
        Assert.Equal(30m, order.TotalPrice);
        Assert.Equal(ServiceStatus.Diagnosing, order.Status);
    }

    [Fact]
    public void RemoveServiceJob_OpenJob_RemovesIt()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Open Job", "desc", 10m);
        order.AttachServiceJob(job, employeeId);

        order.RemoveServiceJob(job.Id, employeeId);

        Assert.Empty(order.ServiceJobs);
        Assert.Equal(0m, order.TotalPrice);
    }

    [Fact]
    public void RequestApproval_WithAcknowledgedJobs_TransitionsToWaitingForApproval()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        order.AttachServiceJob(MakeAcknowledgedJob(order.Id), employeeId);

        order.RequestApproval(employeeId);

        Assert.Equal(ServiceStatus.WaitingForApproval, order.Status);
    }

    [Fact]
    public void Approve_FromWaitingForApproval_TransitionsToExecuting()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        order.AttachServiceJob(MakeAcknowledgedJob(order.Id), employeeId);
        order.RequestApproval(employeeId);

        order.Approve();

        Assert.Equal(ServiceStatus.Executing, order.Status);
    }

    [Fact]
    public void TryFinish_WhenAllJobsCompleted_TransitionsToFinished()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var employee = new Employee("Tech", "tech@shop.com", "hash", UserRole.Mechanic);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Job", "desc", 10m);
        order.AttachServiceJob(job, employeeId);
        job.Acknowledge(employee);
        order.RequestApproval(employeeId);
        order.Approve();
        job.StartProgress(employee.Id);
        job.Complete(employee.Id);

        var finished = order.TryFinish();

        Assert.True(finished);
        Assert.Equal(ServiceStatus.Finished, order.Status);
    }

    [Fact]
    public void Deliver_WhenFinished_TransitionsToDelivered()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var employee = new Employee("Tech", "tech@shop.com", "hash", UserRole.Mechanic);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Job", "desc", 10m);
        order.AttachServiceJob(job, employeeId);
        job.Acknowledge(employee);
        order.RequestApproval(employeeId);
        order.Approve();
        job.StartProgress(employee.Id);
        job.Complete(employee.Id);
        order.TryFinish();

        order.Deliver(employeeId);

        Assert.Equal(ServiceStatus.Delivered, order.Status);
    }
}
