using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class ServiceOrder : BaseEntity
{
    public Guid VehicleId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid AssignedUserId { get; private set; }
    public ServiceStatus Status { get; private set; }
    public decimal TotalPrice { get; private set; }

    public Vehicle Vehicle { get; private set; } = null!;
    public Customer Customer { get; private set; } = null!;
    public User AssignedUser { get; private set; } = null!;

    private readonly List<ServiceOrderItem> _serviceItems = new();
    public IReadOnlyCollection<ServiceOrderItem> ServiceItems => _serviceItems.AsReadOnly();

    private readonly List<ServiceOrderJob> _serviceJobs = new();
    public IReadOnlyCollection<ServiceOrderJob> ServiceJobs => _serviceJobs.AsReadOnly();

    private readonly List<ServiceStatusHistory> _statusHistory = new();
    public IReadOnlyCollection<ServiceStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private ServiceOrder() { }

    /// <summary>
    /// Opens a new service. The employee is auto-assigned.
    /// Vehicle and Customer are fixed at this point and cannot be changed.
    /// </summary>
    public ServiceOrder(Guid vehicleId, Guid customerId, Guid assignedUserId)
    {
        VehicleId = vehicleId;
        CustomerId = customerId;
        AssignedUserId = assignedUserId;
        Status = ServiceStatus.Received;
        TotalPrice = 0;
        SetCreatedBy(assignedUserId);
        _statusHistory.Add(ServiceStatusHistory.CreateInitial(Id, assignedUserId));
    }

    /// <summary>
    /// Adds a service item (with quantity) to the service.
    /// Only allowed while the service is in Received or Diagnosing status.
    /// Only the assigned employee may do this.
    /// Auto-transitions from Received to Diagnosing on the first insertion.
    /// </summary>
    public void AddServiceItem(ServiceOrderItem item, Guid requestingUserId)
    {
        EnsureCanModify(requestingUserId);

        _serviceItems.Add(item);
        RecalculateTotal();

        AutoTransitionToDiagnosing(requestingUserId);
        SetUpdatedBy(requestingUserId);
    }

    /// <summary>
    /// Removes a service item. Only allowed while Diagnosing.
    /// Only the assigned employee may do this.
    /// </summary>
    public void RemoveServiceItem(Guid serviceItemId, Guid requestingUserId)
    {
        EnsureCanModify(requestingUserId);

        if (Status != ServiceStatus.Diagnosing)
            throw new InvalidOperationException("Items can only be removed while the service is in Diagnosing status.");

        var item = _serviceItems.FirstOrDefault(i => i.Id == serviceItemId)
            ?? throw new InvalidOperationException("Service item not found.");

        _serviceItems.Remove(item);
        RecalculateTotal();
        SetUpdatedBy(requestingUserId);
    }

    /// <summary>
    /// Attaches a new ServiceJob to this service order.
    /// Only allowed while the service is in Received or Diagnosing status.
    /// Only the assigned employee may do this.
    /// Auto-transitions from Received to Diagnosing on the first insertion.
    /// </summary>
    public void AttachServiceJob(ServiceOrderJob job, Guid requestingUserId)
    {
        EnsureCanModify(requestingUserId);

        _serviceJobs.Add(job);
        RecalculateTotal();

        AutoTransitionToDiagnosing(requestingUserId);
        SetUpdatedBy(requestingUserId);
    }

    public void RemoveServiceJob(Guid serviceJobId, Guid requestingUserId)
    {
        EnsureCanModify(requestingUserId);

        if (Status != ServiceStatus.Diagnosing)
            throw new InvalidOperationException("Jobs can only be removed while the service is in Diagnosing status.");

        var job = _serviceJobs.FirstOrDefault(i => i.Id == serviceJobId)
            ?? throw new InvalidOperationException("Service job not found.");

        if (job.Status != JobStatus.Open)
            throw new InvalidOperationException("Only open jobs can be removed.");

        _serviceJobs.Remove(job);
        RecalculateTotal();
        SetUpdatedBy(requestingUserId);
    }

    /// <summary>
    /// Requests customer approval. All jobs must be Acknowledged (or Completed).
    /// Only allowed from Diagnosing status by the assigned employee.
    /// </summary>
    public void RequestApproval(Guid requestingUserId)
    {
        if (Status != ServiceStatus.Diagnosing)
            throw new InvalidOperationException("Approval can only be requested when the service is in Diagnosing status.");

        if (requestingUserId != AssignedUserId)
            throw new InvalidOperationException("Only the assigned employee can request approval.");

        if (!_serviceJobs.Any())
            throw new InvalidOperationException("The service must have at least one job before requesting approval.");

        if (_serviceJobs.Any(j => j.Status == JobStatus.Open))
            throw new InvalidOperationException("All jobs must be acknowledged before requesting approval.");

        TransitionToStatus(ServiceStatus.WaitingForApproval, requestingUserId);
    }

    /// <summary>
    /// Customer approves the service. Transitions from WaitingForApproval to Executing.
    /// </summary>
    public void Approve()
    {
        if (Status != ServiceStatus.WaitingForApproval)
            throw new InvalidOperationException("Service can only be approved when it is waiting for approval.");

        TransitionToStatus(ServiceStatus.Executing, null);
    }

    /// <summary>
    /// Customer rejects the service estimate. Transitions from WaitingForApproval back to Diagnosing
    /// so the assigned employee can revise the estimate and request approval again.
    /// </summary>
    public void Reject()
    {
        if (Status != ServiceStatus.WaitingForApproval)
            throw new InvalidOperationException("Service can only be rejected when it is waiting for approval.");

        TransitionToStatus(ServiceStatus.Diagnosing, null);
    }

    /// <summary>
    /// Called after a job is completed. Checks if all jobs are done and auto-transitions to Finished.
    /// Returns true if the service transitioned to Finished.
    /// </summary>
    public bool TryFinish()
    {
        if (Status != ServiceStatus.Executing)
            return false;

        if (!_serviceJobs.Any())
            return false;

        if (_serviceJobs.All(j => j.Status == JobStatus.Completed))
        {
            TransitionToStatus(ServiceStatus.Finished, null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Delivers the service to the customer. Transitions from Finished to Delivered.
    /// </summary>
    public void Deliver(Guid requestingUserId)
    {
        if (Status != ServiceStatus.Finished)
            throw new InvalidOperationException("Service can only be delivered when it is finished.");

        TransitionToStatus(ServiceStatus.Delivered, requestingUserId);
    }

    /// <summary>
    /// Customer disputes the service. Goes back to Diagnosing so new items and jobs can be added.
    /// New ServiceItems and ServiceJobs can be added; existing records remain unchanged.
    /// </summary>
    public void Dispute(Guid requestingUserId)
    {
        if (Status != ServiceStatus.Finished)
            throw new InvalidOperationException("Service can only be disputed when it is finished.");

        TransitionToStatus(ServiceStatus.Diagnosing, requestingUserId);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void EnsureCanModify(Guid requestingUserId)
    {
        if (Status != ServiceStatus.Received && Status != ServiceStatus.Diagnosing)
            throw new InvalidOperationException(
                "Items and jobs can only be added while the service is in Received or Diagnosing status.");

        if (requestingUserId != AssignedUserId)
            throw new InvalidOperationException("Only the assigned employee can add items or jobs to this service.");
    }

    private void AutoTransitionToDiagnosing(Guid userId)
    {
        if (Status == ServiceStatus.Received)
            TransitionToStatus(ServiceStatus.Diagnosing, userId);
    }

    private void TransitionToStatus(ServiceStatus newStatus, Guid? userId)
    {
        var previous = Status;
        Status = newStatus;
        _statusHistory.Add(ServiceStatusHistory.CreateTransition(Id, previous, newStatus, userId));
        SetUpdatedAt();
    }

    private void RecalculateTotal()
    {
        TotalPrice = _serviceItems.Sum(i => i.Price * i.Quantity) + _serviceJobs.Sum(j => j.Price);
    }
}
