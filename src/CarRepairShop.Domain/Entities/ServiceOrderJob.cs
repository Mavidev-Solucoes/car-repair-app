using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class ServiceOrderJob : BaseEntity
{
    public Guid ServiceOrderId { get; private set; }
    public Guid ServiceJobId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public JobStatus Status { get; private set; } = JobStatus.Open;
    public Guid? AssignedUserId { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;
    public ServiceJob ServiceJob { get; private set; } = null!;
    public User? AssignedUser { get; private set; }
    public ICollection<ServiceOrderJobStatusHistory> StatusHistory { get; private set; } = new List<ServiceOrderJobStatusHistory>();

    private ServiceOrderJob() { }

    public ServiceOrderJob(
        Guid serviceOrderId,
        Guid serviceJobId,
        string name,
        string description,
        decimal price,
        Guid? createdUserId = null)
    {
        ServiceOrderId = serviceOrderId;
        ServiceJobId = serviceJobId;
        Name = name;
        Description = description;
        Price = price;
        SetCreatedBy(createdUserId);

        StatusHistory.Add(ServiceOrderJobStatusHistory.CreateInitial(Id));
    }

    public void Acknowledge(Employee employee)
    {
        if (Status != JobStatus.Open)
            throw new InvalidOperationException("Job can only be acknowledged when it is Open.");

        var previous = Status;
        Status = JobStatus.Acknowledged;
        AssignedUserId = employee.Id;
        SetUpdatedBy(employee.Id);

        StatusHistory.Add(ServiceOrderJobStatusHistory.CreateTransition(Id, previous, Status, employee.Id));
    }

    public void StartProgress(Guid userId)
    {
        if (Status != JobStatus.Acknowledged)
            throw new InvalidOperationException("Job can only be started when it is Acknowledged.");

        if (AssignedUserId != userId)
            throw new InvalidOperationException("Only the assigned user can start progress on this job.");

        var previous = Status;
        Status = JobStatus.InProgress;
        SetUpdatedBy(userId);

        StatusHistory.Add(ServiceOrderJobStatusHistory.CreateTransition(Id, previous, Status, userId));
    }

    public void Complete(Guid userId)
    {
        if (Status != JobStatus.InProgress)
            throw new InvalidOperationException("Job can only be completed when it is In Progress.");

        if (AssignedUserId != userId)
            throw new InvalidOperationException("Only the assigned user can complete this job.");

        var previous = Status;
        Status = JobStatus.Completed;
        SetUpdatedBy(userId);

        StatusHistory.Add(ServiceOrderJobStatusHistory.CreateTransition(Id, previous, Status, userId));
    }
}
