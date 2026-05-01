using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class ServiceJob : ServiceCatalogBase
{
    public Guid ServiceOrderId { get; private set; }
    public JobStatus Status { get; private set; } = JobStatus.Open;
    public Guid? AssignedUserId { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;
    public User? AssignedUser { get; private set; }
    public ICollection<ServiceJobStatusHistory> StatusHistory { get; private set; } = new List<ServiceJobStatusHistory>();

    private ServiceJob() { }

    public ServiceJob(Guid serviceOrderId, string name, string description, int unitCost, Guid? createdUserId = null)
    {
        ServiceOrderId = serviceOrderId;
        Name = name;
        Description = description;
        UnitCost = unitCost;
        Status = JobStatus.Open;
        SetCreatedBy(createdUserId);

        StatusHistory.Add(ServiceJobStatusHistory.CreateInitial(Id));
    }

    public void Update(string name, string description, int unitCost, Guid? updatedUserId = null)
    {
        Name = name;
        Description = description;
        UnitCost = unitCost;
        SetUpdatedBy(updatedUserId);
    }

    public void Acknowledge(User employee)
    {
        if (Status != JobStatus.Open)
            throw new InvalidOperationException("Job can only be acknowledged when it is Open.");

        if (employee.UserType != UserType.Employee)
            throw new InvalidOperationException("Only employees can acknowledge a job.");

        var previous = Status;
        Status = JobStatus.Acknowledged;
        AssignedUserId = employee.Id;
        SetUpdatedBy(employee.Id);

        StatusHistory.Add(ServiceJobStatusHistory.CreateTransition(Id, previous, Status, employee.Id));
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

        StatusHistory.Add(ServiceJobStatusHistory.CreateTransition(Id, previous, Status, userId));
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

        StatusHistory.Add(ServiceJobStatusHistory.CreateTransition(Id, previous, Status, userId));
    }
}
