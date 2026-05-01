using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class ServiceStatusHistory : BaseEntity
{
    public Guid ServiceOrderId { get; private set; }
    public ServiceStatus? FromStatus { get; private set; }
    public ServiceStatus ToStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    public ServiceOrder? ServiceOrder { get; private set; }

    private ServiceStatusHistory() { }

    internal static ServiceStatusHistory CreateInitial(Guid serviceOrderId, Guid? changedByUserId) =>
        new()
        {
            ServiceOrderId = serviceOrderId,
            FromStatus = null,
            ToStatus = ServiceStatus.Received,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = changedByUserId
        };

    internal static ServiceStatusHistory CreateTransition(
        Guid serviceOrderId,
        ServiceStatus from,
        ServiceStatus to,
        Guid? changedByUserId) =>
        new()
        {
            ServiceOrderId = serviceOrderId,
            FromStatus = from,
            ToStatus = to,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = changedByUserId
        };
}
