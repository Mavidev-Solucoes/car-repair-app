namespace CarRepairShop.Domain.Entities;

public class ServiceItem : BaseEntity
{
    public Guid ServiceOrderId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;

    private ServiceItem() { }

    public ServiceItem(Guid serviceOrderId, string description, decimal price, int quantity)
    {
        ServiceOrderId = serviceOrderId;
        Description = description;
        Price = price;
        Quantity = quantity;
    }

    public void Update(string description, decimal price, int quantity)
    {
        Description = description;
        Price = price;
        Quantity = quantity;
        SetUpdatedAt();
    }
}
