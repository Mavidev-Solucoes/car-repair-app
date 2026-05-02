namespace CarRepairShop.Domain.Entities;

public class ServiceOrderItem : BaseEntity
{
    public Guid ServiceOrderId { get; private set; }
    public Guid ServiceItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;
    public ServiceItem ServiceItem { get; private set; } = null!;

    private ServiceOrderItem() { }

    public ServiceOrderItem(Guid serviceOrderId, Guid serviceItemId, string description, decimal price, int quantity)
    {
        ServiceOrderId = serviceOrderId;
        ServiceItemId = serviceItemId;
        Description = description;
        Price = price;
        Quantity = quantity;
    }
}
