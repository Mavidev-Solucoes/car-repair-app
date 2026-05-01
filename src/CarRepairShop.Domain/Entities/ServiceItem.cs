namespace CarRepairShop.Domain.Entities;

public class ServiceItem : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int UnitCost { get; private set; }

    private ServiceItem() { }

    public ServiceItem(string name, string description, int unitCost, Guid? createdUserId = null)
    {
        Name = name;
        Description = description;
        UnitCost = unitCost;
        SetCreatedBy(createdUserId);
    }

    public void Update(string name, string description, int unitCost, Guid? updatedUserId = null)
    {
        Name = name;
        Description = description;
        UnitCost = unitCost;
        SetUpdatedBy(updatedUserId);
    }
}
