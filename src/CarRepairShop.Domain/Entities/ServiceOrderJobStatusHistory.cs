using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class ServiceOrderJobStatusHistory : BaseEntity
{
    public Guid ServiceOrderJobId { get; private set; }
    public JobStatus? FromStatus { get; private set; }
    public JobStatus ToStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    public ServiceOrderJob? ServiceOrderJob { get; private set; }

    private ServiceOrderJobStatusHistory() { }

    internal static ServiceOrderJobStatusHistory CreateInitial(Guid serviceOrderJobId) =>
        new()
        {
            ServiceOrderJobId = serviceOrderJobId,
            ToStatus = JobStatus.Open,
            ChangedAt = DateTime.UtcNow
        };

    internal static ServiceOrderJobStatusHistory CreateTransition(
        Guid serviceOrderJobId,
        JobStatus from,
        JobStatus to,
        Guid? changedByUserId) =>
        new()
        {
            ServiceOrderJobId = serviceOrderJobId,
            FromStatus = from,
            ToStatus = to,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = changedByUserId
        };
}
