namespace CarRepairShop.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public Guid? CreatedUserId { get; protected set; }
    public Guid? LastUpdatedUserId { get; protected set; }

    protected void SetCreatedBy(Guid? userId) => CreatedUserId = userId;

    protected void SetUpdatedBy(Guid? userId)
    {
        LastUpdatedUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    protected void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}
