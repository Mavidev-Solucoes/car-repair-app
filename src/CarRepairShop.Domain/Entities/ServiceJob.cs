namespace CarRepairShop.Domain.Entities;

public class ServiceJob : ServiceCatalogBase
{
    private ServiceJob() { }

    public ServiceJob(string name, string description, decimal price, Guid? createdUserId = null)
    {
        Name = name;
        Description = description;
        Price = price;
        SetCreatedBy(createdUserId);
    }

    public void Update(string name, string description, decimal price, Guid? updatedUserId = null)
    {
        Name = name;
        Description = description;
        Price = price;
        SetUpdatedBy(updatedUserId);
    }
}
