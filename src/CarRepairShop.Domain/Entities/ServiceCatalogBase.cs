namespace CarRepairShop.Domain.Entities;

public abstract class ServiceCatalogBase : BaseEntity
{
    public string Name { get; protected set; } = string.Empty;
    public string Description { get; protected set; } = string.Empty;
    public decimal Price { get; protected set; }
}
