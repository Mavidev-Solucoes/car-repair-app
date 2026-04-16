using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class ServiceOrder : BaseEntity
{
    public Guid VehicleId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ServiceOrderStatus Status { get; private set; }
    public decimal TotalPrice { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Notes { get; private set; }

    public Vehicle Vehicle { get; private set; } = null!;

    private readonly List<ServiceItem> _serviceItems = new();
    public IReadOnlyCollection<ServiceItem> ServiceItems => _serviceItems.AsReadOnly();

    private ServiceOrder() { }

    public ServiceOrder(Guid vehicleId, string description, string? notes = null)
    {
        VehicleId = vehicleId;
        Description = description;
        Status = ServiceOrderStatus.Open;
        TotalPrice = 0;
        Notes = notes;
    }

    public void UpdateStatus(ServiceOrderStatus status)
    {
        Status = status;
        if (status == ServiceOrderStatus.Completed)
            CompletedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void UpdateDescription(string description, string? notes)
    {
        Description = description;
        Notes = notes;
        SetUpdatedAt();
    }

    public void AddServiceItem(ServiceItem item)
    {
        _serviceItems.Add(item);
        RecalculateTotal();
        SetUpdatedAt();
    }

    public void RemoveServiceItem(Guid serviceItemId)
    {
        var item = _serviceItems.FirstOrDefault(i => i.Id == serviceItemId);
        if (item is not null)
        {
            _serviceItems.Remove(item);
            RecalculateTotal();
            SetUpdatedAt();
        }
    }

    private void RecalculateTotal()
    {
        TotalPrice = _serviceItems.Sum(i => i.Price * i.Quantity);
    }
}
