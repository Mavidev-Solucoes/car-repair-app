using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class ServiceJobStatusHistory : BaseEntity
{
    public Guid ServiceJobId { get; private set; }
    public JobStatus? FromStatus { get; private set; }
    public JobStatus ToStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    public ServiceJob? ServiceJob { get; private set; }

    private ServiceJobStatusHistory() { }

    internal static ServiceJobStatusHistory CreateInitial(Guid serviceJobId) =>
        new()
        {
            ServiceJobId = serviceJobId,
            FromStatus = null,
            ToStatus = JobStatus.Open,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = null
        };

    internal static ServiceJobStatusHistory CreateTransition(
        Guid serviceJobId,
        JobStatus from,
        JobStatus to,
        Guid? changedByUserId) =>
        new()
        {
            ServiceJobId = serviceJobId,
            FromStatus = from,
            ToStatus = to,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = changedByUserId
        };
}
