using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests.Domain;

public class ServiceJobTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var serviceOrderId = Guid.NewGuid();
        var createdUserId = Guid.NewGuid();

        var job = new ServiceJob(serviceOrderId, "Oil Change", "Engine oil change", 5000, createdUserId);

        Assert.Equal(serviceOrderId, job.ServiceOrderId);
        Assert.Equal("Oil Change", job.Name);
        Assert.Equal("Engine oil change", job.Description);
        Assert.Equal(5000, job.UnitCost);
        Assert.Equal(JobStatus.Open, job.Status);
        Assert.Equal(createdUserId, job.CreatedUserId);
        Assert.Null(job.AssignedUserId);
    }

    [Fact]
    public void Constructor_WithoutCreatedUserId_SetsNullCreatedUserId()
    {
        var serviceOrderId = Guid.NewGuid();

        var job = new ServiceJob(serviceOrderId, "Oil Change", "Engine oil change", 5000);

        Assert.Null(job.CreatedUserId);
        Assert.Equal(JobStatus.Open, job.Status);
    }

    [Fact]
    public void Constructor_AddsInitialStatusHistory()
    {
        var serviceOrderId = Guid.NewGuid();

        var job = new ServiceJob(serviceOrderId, "Oil Change", "Engine oil change", 5000);

        Assert.Single(job.StatusHistory);
        var history = job.StatusHistory.First();
        Assert.Null(history.FromStatus);
        Assert.Equal(JobStatus.Open, history.ToStatus);
        Assert.Equal(job.Id, history.ServiceJobId);
    }

    [Fact]
    public void Constructor_GeneratesNewId()
    {
        var serviceOrderId = Guid.NewGuid();

        var job1 = new ServiceJob(serviceOrderId, "Job 1", "Desc 1", 1000);
        var job2 = new ServiceJob(serviceOrderId, "Job 2", "Desc 2", 2000);

        Assert.NotEqual(job1.Id, job2.Id);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ChangesNameDescriptionAndUnitCost()
    {
        var serviceOrderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Old Name", "Old Desc", 100);

        job.Update("New Name", "New Desc", 200, userId);

        Assert.Equal("New Name", job.Name);
        Assert.Equal("New Desc", job.Description);
        Assert.Equal(200, job.UnitCost);
        Assert.Equal(userId, job.LastUpdatedUserId);
        Assert.NotNull(job.UpdatedAt);
    }

    [Fact]
    public void Update_WithoutUserId_SetsNullLastUpdatedUserId()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Old Name", "Old Desc", 100);

        job.Update("New Name", "New Desc", 200);

        Assert.Equal("New Name", job.Name);
        Assert.Equal(200, job.UnitCost);
        Assert.Null(job.LastUpdatedUserId);
    }

    // ── Acknowledge ──────────────────────────────────────────────────────────

    [Fact]
    public void Acknowledge_WhenOpenAndEmployee_SetsAcknowledgedStatus()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Brake Repair", "Fix brakes", 3000);
        var employee = new User("Alice", "alice@shop.com", "hash", UserRole.Admin, UserType.Employee);

        job.Acknowledge(employee);

        Assert.Equal(JobStatus.Acknowledged, job.Status);
        Assert.Equal(employee.Id, job.AssignedUserId);
        Assert.Equal(employee.Id, job.LastUpdatedUserId);
    }

    [Fact]
    public void Acknowledge_WhenOpenAndEmployee_AddsStatusHistory()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Brake Repair", "Fix brakes", 3000);
        var employee = new User("Alice", "alice@shop.com", "hash", UserRole.Admin, UserType.Employee);

        job.Acknowledge(employee);

        Assert.Equal(2, job.StatusHistory.Count);
        var transition = job.StatusHistory.Last();
        Assert.Equal(JobStatus.Open, transition.FromStatus);
        Assert.Equal(JobStatus.Acknowledged, transition.ToStatus);
        Assert.Equal(employee.Id, transition.ChangedByUserId);
    }

    [Fact]
    public void Acknowledge_WhenStatusIsNotOpen_ThrowsInvalidOperationException()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Brake Repair", "Fix brakes", 3000);
        var employee = new User("Alice", "alice@shop.com", "hash", UserRole.Admin, UserType.Employee);
        job.Acknowledge(employee);  // now Acknowledged

        var ex = Assert.Throws<InvalidOperationException>(() => job.Acknowledge(employee));
        Assert.Contains("Open", ex.Message);
    }

    [Fact]
    public void Acknowledge_WhenUserIsEndUser_ThrowsInvalidOperationException()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Brake Repair", "Fix brakes", 3000);
        var endUser = new User("Bob", "bob@customer.com", "hash", UserRole.Admin, UserType.EndUser);

        var ex = Assert.Throws<InvalidOperationException>(() => job.Acknowledge(endUser));
        Assert.Contains("employees", ex.Message);
    }

    // ── StartProgress ────────────────────────────────────────────────────────

    [Fact]
    public void StartProgress_WhenAcknowledgedAndAssignedUser_SetsInProgressStatus()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Engine Repair", "Fix engine", 10000);
        var employee = new User("Carol", "carol@shop.com", "hash", UserRole.Admin, UserType.Employee);
        job.Acknowledge(employee);

        job.StartProgress(employee.Id);

        Assert.Equal(JobStatus.InProgress, job.Status);
        Assert.Equal(employee.Id, job.LastUpdatedUserId);
    }

    [Fact]
    public void StartProgress_WhenAcknowledgedAndAssignedUser_AddsStatusHistory()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Engine Repair", "Fix engine", 10000);
        var employee = new User("Carol", "carol@shop.com", "hash", UserRole.Admin, UserType.Employee);
        job.Acknowledge(employee);

        job.StartProgress(employee.Id);

        Assert.Equal(3, job.StatusHistory.Count);
        var transition = job.StatusHistory.Last();
        Assert.Equal(JobStatus.Acknowledged, transition.FromStatus);
        Assert.Equal(JobStatus.InProgress, transition.ToStatus);
        Assert.Equal(employee.Id, transition.ChangedByUserId);
    }

    [Fact]
    public void StartProgress_WhenStatusIsNotAcknowledged_ThrowsInvalidOperationException()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Engine Repair", "Fix engine", 10000);
        var userId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(() => job.StartProgress(userId));
        Assert.Contains("Acknowledged", ex.Message);
    }

    [Fact]
    public void StartProgress_WhenUserIsNotAssigned_ThrowsInvalidOperationException()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Engine Repair", "Fix engine", 10000);
        var employee = new User("Carol", "carol@shop.com", "hash", UserRole.Admin, UserType.Employee);
        var otherUserId = Guid.NewGuid();
        job.Acknowledge(employee);

        var ex = Assert.Throws<InvalidOperationException>(() => job.StartProgress(otherUserId));
        Assert.Contains("assigned user", ex.Message);
    }

    // ── Complete ─────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_WhenInProgressAndAssignedUser_SetsCompletedStatus()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Tire Change", "Replace all tires", 4000);
        var employee = new User("Dave", "dave@shop.com", "hash", UserRole.Admin, UserType.Employee);
        job.Acknowledge(employee);
        job.StartProgress(employee.Id);

        job.Complete(employee.Id);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(employee.Id, job.LastUpdatedUserId);
    }

    [Fact]
    public void Complete_WhenInProgressAndAssignedUser_AddsStatusHistory()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Tire Change", "Replace all tires", 4000);
        var employee = new User("Dave", "dave@shop.com", "hash", UserRole.Admin, UserType.Employee);
        job.Acknowledge(employee);
        job.StartProgress(employee.Id);

        job.Complete(employee.Id);

        Assert.Equal(4, job.StatusHistory.Count);
        var transition = job.StatusHistory.Last();
        Assert.Equal(JobStatus.InProgress, transition.FromStatus);
        Assert.Equal(JobStatus.Completed, transition.ToStatus);
        Assert.Equal(employee.Id, transition.ChangedByUserId);
    }

    [Fact]
    public void Complete_WhenStatusIsNotInProgress_ThrowsInvalidOperationException()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Tire Change", "Replace all tires", 4000);
        var employee = new User("Dave", "dave@shop.com", "hash", UserRole.Admin, UserType.Employee);
        job.Acknowledge(employee);  // Acknowledged, not InProgress

        var ex = Assert.Throws<InvalidOperationException>(() => job.Complete(employee.Id));
        Assert.Contains("In Progress", ex.Message);
    }

    [Fact]
    public void Complete_WhenUserIsNotAssigned_ThrowsInvalidOperationException()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Tire Change", "Replace all tires", 4000);
        var employee = new User("Dave", "dave@shop.com", "hash", UserRole.Admin, UserType.Employee);
        var otherUserId = Guid.NewGuid();
        job.Acknowledge(employee);
        job.StartProgress(employee.Id);

        var ex = Assert.Throws<InvalidOperationException>(() => job.Complete(otherUserId));
        Assert.Contains("assigned user", ex.Message);
    }

    // ── Full lifecycle ───────────────────────────────────────────────────────

    [Fact]
    public void FullLifecycle_OpenToCompleted_WorksCorrectly()
    {
        var serviceOrderId = Guid.NewGuid();
        var job = new ServiceJob(serviceOrderId, "Full Service", "Complete service", 20000);
        var employee = new User("Eve", "eve@shop.com", "hash", UserRole.Admin, UserType.Employee);

        Assert.Equal(JobStatus.Open, job.Status);

        job.Acknowledge(employee);
        Assert.Equal(JobStatus.Acknowledged, job.Status);

        job.StartProgress(employee.Id);
        Assert.Equal(JobStatus.InProgress, job.Status);

        job.Complete(employee.Id);
        Assert.Equal(JobStatus.Completed, job.Status);

        Assert.Equal(4, job.StatusHistory.Count);
    }
}
