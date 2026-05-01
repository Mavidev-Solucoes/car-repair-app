namespace CarRepairShop.Domain.Entities;

public class ServiceItem : ServiceCatalogBase
{
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
