namespace CarRepairShop.Domain.Entities;

public class ServiceItem : ServiceCatalogBase
{
    public int Stock { get; private set; }

    private ServiceItem() { }

    public ServiceItem(string name, string description, decimal price, int stock, Guid? createdUserId = null)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        SetCreatedBy(createdUserId);
    }

    public void Update(string name, string description, decimal price, int stock, Guid? updatedUserId = null)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        SetUpdatedBy(updatedUserId);
    }

    public void ReserveStock(int quantity, Guid? updatedUserId = null)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");

        if (Stock < quantity)
            throw new InvalidOperationException($"Insufficient stock for item '{Name}'. Available: {Stock}, requested: {quantity}.");

        Stock -= quantity;
        SetUpdatedBy(updatedUserId);
    }

    public void RestoreStock(int quantity, Guid? updatedUserId = null)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");

        Stock += quantity;
        SetUpdatedBy(updatedUserId);
    }
}
