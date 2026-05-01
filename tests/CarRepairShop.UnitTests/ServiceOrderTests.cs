using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests;

public class ServiceOrderTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ServiceOrder CreateOrder(out Guid vehicleId, out Guid customerId, out Guid employeeId)
    {
        vehicleId = Guid.NewGuid();
        customerId = Guid.NewGuid();
        employeeId = Guid.NewGuid();
        return new ServiceOrder(vehicleId, customerId, employeeId);
    }

    private static ServiceOrderItem MakeItem(Guid serviceOrderId, decimal price = 100m, int quantity = 2) =>
        new(serviceOrderId, "Oil change", price, quantity);

    private static ServiceJob MakeAcknowledgedJob(Guid serviceOrderId)
    {
        var job = new ServiceJob(serviceOrderId, "Brake inspection", "Check brake pads", 50);
        var employee = new User("Tech", "tech@shop.com", "hash", UserRole.Mechanic, UserType.Employee);
        job.Acknowledge(employee);
        return job;
    }

    // ── Constructor ───────────────────────────────────────────────────────────

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
    public void Constructor_SeedsInitialStatusHistory()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        Assert.Single(order.StatusHistory);
        var entry = order.StatusHistory.First();
        Assert.Null(entry.FromStatus);
        Assert.Equal(ServiceStatus.Received, entry.ToStatus);
        Assert.Equal(employeeId, entry.ChangedByUserId);
    }

    [Fact]
    public void Constructor_StartsWithNoItemsOrJobs()
    {
        var order = CreateOrder(out _, out _, out _);

        Assert.Empty(order.ServiceItems);
        Assert.Empty(order.ServiceJobs);
    }

    // ── AddServiceItem ────────────────────────────────────────────────────────

    [Fact]
    public void AddServiceItem_AddsItemAndUpdatesTotal()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var item = MakeItem(order.Id, 50m, 3);

        order.AddServiceItem(item, employeeId);

        Assert.Single(order.ServiceItems);
        Assert.Equal(150m, order.TotalPrice);
    }

    [Fact]
    public void AddServiceItem_AutoTransitionsToDiagnosing()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        Assert.Equal(ServiceStatus.Received, order.Status);

        order.AddServiceItem(MakeItem(order.Id), employeeId);

        Assert.Equal(ServiceStatus.Diagnosing, order.Status);
    }

    [Fact]
    public void AddServiceItem_AddsTransitionToStatusHistory()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        order.AddServiceItem(MakeItem(order.Id), employeeId);

        // Initial + Received→Diagnosing
        Assert.Equal(2, order.StatusHistory.Count);
        var transition = order.StatusHistory.Last();
        Assert.Equal(ServiceStatus.Received, transition.FromStatus);
        Assert.Equal(ServiceStatus.Diagnosing, transition.ToStatus);
    }

    [Fact]
    public void AddServiceItem_WhileDiagnosing_DoesNotAddExtraTransition()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        order.AddServiceItem(MakeItem(order.Id), employeeId); // triggers Received→Diagnosing

        var historyCountBefore = order.StatusHistory.Count;
        order.AddServiceItem(MakeItem(order.Id, 10m, 1), employeeId);

        Assert.Equal(historyCountBefore, order.StatusHistory.Count);
    }

    [Fact]
    public void AddServiceItem_WrongUser_Throws()
    {
        var order = CreateOrder(out _, out _, out _);

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.AddServiceItem(MakeItem(order.Id), Guid.NewGuid()));

        Assert.Contains("Only the assigned employee", ex.Message);
    }

    [Fact]
    public void AddServiceItem_WrongStatus_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job, employeeId);
        order.RequestApproval(employeeId);
        // Status is now WaitingForApproval

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.AddServiceItem(MakeItem(order.Id), employeeId));

        Assert.Contains("Received or Diagnosing", ex.Message);
    }

    // ── RemoveServiceItem ─────────────────────────────────────────────────────

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
    public void RemoveServiceItem_WrongStatus_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        // Status is Received — not Diagnosing
        var item = new ServiceOrderItem(order.Id, "Part", 50m, 1);

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.RemoveServiceItem(item.Id, employeeId));

        Assert.Contains("Diagnosing", ex.Message);
    }

    [Fact]
    public void RemoveServiceItem_ItemNotFound_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        order.AddServiceItem(MakeItem(order.Id), employeeId); // now Diagnosing

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.RemoveServiceItem(Guid.NewGuid(), employeeId));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void RemoveServiceItem_WrongUser_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var item = MakeItem(order.Id);
        order.AddServiceItem(item, employeeId); // now Diagnosing

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.RemoveServiceItem(item.Id, Guid.NewGuid()));

        Assert.Contains("Only the assigned employee", ex.Message);
    }

    // ── AttachServiceJob ──────────────────────────────────────────────────────

    [Fact]
    public void AttachServiceJob_AddsJob()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = new ServiceJob(order.Id, "Tire rotation", "Rotate all four tires", 30);

        order.AttachServiceJob(job, employeeId);

        Assert.Single(order.ServiceJobs);
    }

    [Fact]
    public void AttachServiceJob_AutoTransitionsToDiagnosing()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        order.AttachServiceJob(new ServiceJob(order.Id, "Job", "desc", 10), employeeId);

        Assert.Equal(ServiceStatus.Diagnosing, order.Status);
    }

    [Fact]
    public void AttachServiceJob_WrongUser_Throws()
    {
        var order = CreateOrder(out _, out _, out _);

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.AttachServiceJob(new ServiceJob(order.Id, "Job", "desc", 10), Guid.NewGuid()));

        Assert.Contains("Only the assigned employee", ex.Message);
    }

    [Fact]
    public void AttachServiceJob_WrongStatus_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job, employeeId);
        order.RequestApproval(employeeId);

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.AttachServiceJob(new ServiceJob(order.Id, "Job2", "desc", 10), employeeId));

        Assert.Contains("Received or Diagnosing", ex.Message);
    }

    // ── RequestApproval ───────────────────────────────────────────────────────

    [Fact]
    public void RequestApproval_TransitionsToWaitingForApproval()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job, employeeId);

        order.RequestApproval(employeeId);

        Assert.Equal(ServiceStatus.WaitingForApproval, order.Status);
    }

    [Fact]
    public void RequestApproval_WrongStatus_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.RequestApproval(employeeId));

        Assert.Contains("Diagnosing", ex.Message);
    }

    [Fact]
    public void RequestApproval_WrongUser_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        order.AttachServiceJob(new ServiceJob(order.Id, "Job", "desc", 10), employeeId);

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.RequestApproval(Guid.NewGuid()));

        Assert.Contains("Only the assigned employee", ex.Message);
    }

    [Fact]
    public void RequestApproval_NoJobs_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        // Transition to Diagnosing via an item, keeping jobs empty
        order.AddServiceItem(MakeItem(order.Id), employeeId);

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.RequestApproval(employeeId));

        Assert.Contains("at least one job", ex.Message);
    }

    [Fact]
    public void RequestApproval_OpenJobs_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        // Attach an Open job (not yet acknowledged)
        var job = new ServiceJob(order.Id, "Open Job", "desc", 10);
        order.AttachServiceJob(job, employeeId);

        var ex = Assert.Throws<InvalidOperationException>(
            () => order.RequestApproval(employeeId));

        Assert.Contains("acknowledged", ex.Message);
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_TransitionsToExecuting()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job, employeeId);
        order.RequestApproval(employeeId);

        order.Approve();

        Assert.Equal(ServiceStatus.Executing, order.Status);
    }

    [Fact]
    public void Approve_WrongStatus_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        var ex = Assert.Throws<InvalidOperationException>(() => order.Approve());

        Assert.Contains("waiting for approval", ex.Message);
    }

    // ── TryFinish ─────────────────────────────────────────────────────────────

    [Fact]
    public void TryFinish_AllJobsCompleted_TransitionsToFinishedAndReturnsTrue()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job, employeeId);
        order.RequestApproval(employeeId);
        order.Approve();

        // Complete the job via its own workflow
        var techId = job.AssignedUserId!.Value;
        job.StartProgress(techId);
        job.Complete(techId);

        var result = order.TryFinish();

        Assert.True(result);
        Assert.Equal(ServiceStatus.Finished, order.Status);
    }

    [Fact]
    public void TryFinish_NotAllJobsCompleted_ReturnsFalse()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job1 = MakeAcknowledgedJob(order.Id);
        var job2 = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job1, employeeId);
        order.AttachServiceJob(job2, employeeId);
        order.RequestApproval(employeeId);
        order.Approve();

        // Only complete job1, leave job2 Acknowledged
        var techId = job1.AssignedUserId!.Value;
        job1.StartProgress(techId);
        job1.Complete(techId);

        var result = order.TryFinish();

        Assert.False(result);
        Assert.Equal(ServiceStatus.Executing, order.Status);
    }

    [Fact]
    public void TryFinish_NotInExecutingStatus_ReturnsFalse()
    {
        var order = CreateOrder(out _, out _, out _);

        var result = order.TryFinish();

        Assert.False(result);
    }

    // ── Deliver ───────────────────────────────────────────────────────────────

    [Fact]
    public void Deliver_TransitionsToDelivered()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job, employeeId);
        order.RequestApproval(employeeId);
        order.Approve();
        var techId = job.AssignedUserId!.Value;
        job.StartProgress(techId);
        job.Complete(techId);
        order.TryFinish();

        order.Deliver(employeeId);

        Assert.Equal(ServiceStatus.Delivered, order.Status);
    }

    [Fact]
    public void Deliver_WrongStatus_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        var ex = Assert.Throws<InvalidOperationException>(() => order.Deliver(employeeId));

        Assert.Contains("finished", ex.Message);
    }

    // ── Dispute ───────────────────────────────────────────────────────────────

    [Fact]
    public void Dispute_TransitionsBackToDiagnosing()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job, employeeId);
        order.RequestApproval(employeeId);
        order.Approve();
        var techId = job.AssignedUserId!.Value;
        job.StartProgress(techId);
        job.Complete(techId);
        order.TryFinish();

        order.Dispute(employeeId);

        Assert.Equal(ServiceStatus.Diagnosing, order.Status);
    }

    [Fact]
    public void Dispute_WrongStatus_Throws()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        var ex = Assert.Throws<InvalidOperationException>(() => order.Dispute(employeeId));

        Assert.Contains("finished", ex.Message);
    }

    // ── TotalPrice recalculation ──────────────────────────────────────────────

    [Fact]
    public void TotalPrice_RecalculatesAfterMultipleItems()
    {
        var order = CreateOrder(out _, out _, out var employeeId);

        order.AddServiceItem(new ServiceOrderItem(order.Id, "Part A", 100m, 2), employeeId);
        order.AddServiceItem(new ServiceOrderItem(order.Id, "Part B", 50m, 1), employeeId);

        Assert.Equal(250m, order.TotalPrice);
    }

    [Fact]
    public void TotalPrice_ZeroAfterRemovingAllItems()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var item = new ServiceOrderItem(order.Id, "Part A", 100m, 2);
        order.AddServiceItem(item, employeeId);

        order.RemoveServiceItem(item.Id, employeeId);

        Assert.Equal(0m, order.TotalPrice);
    }

    // ── Status history completeness ───────────────────────────────────────────

    [Fact]
    public void StatusHistory_TracksFullLifecycle()
    {
        var order = CreateOrder(out _, out _, out var employeeId);
        var job = MakeAcknowledgedJob(order.Id);
        order.AttachServiceJob(job, employeeId);  // Received→Diagnosing
        order.RequestApproval(employeeId);         // Diagnosing→WaitingForApproval
        order.Approve();                            // WaitingForApproval→Executing
        var techId = job.AssignedUserId!.Value;
        job.StartProgress(techId);
        job.Complete(techId);
        order.TryFinish();                         // Executing→Finished
        order.Deliver(employeeId);                 // Finished→Delivered

        // Initial + 5 transitions = 6 entries
        Assert.Equal(6, order.StatusHistory.Count);
        Assert.Equal(ServiceStatus.Delivered, order.StatusHistory.Last().ToStatus);
    }
}
