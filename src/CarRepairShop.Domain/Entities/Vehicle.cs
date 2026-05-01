namespace CarRepairShop.Domain.Entities;

public class Vehicle : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string LicensePlate { get; private set; } = string.Empty;
    public string? Color { get; private set; }

    public Customer Customer { get; private set; } = null!;

    private readonly List<ServiceOrder> _serviceOrders = new();
    public IReadOnlyCollection<ServiceOrder> ServiceOrders => _serviceOrders.AsReadOnly();

    private Vehicle() { }

    public Vehicle(Guid customerId, string brand, string model, int year, string licensePlate, string? color = null, Guid? createdUserId = null)
    {
        CustomerId = customerId;
        Brand = brand;
        Model = model;
        Year = year;
        LicensePlate = StripDashes(licensePlate).ToUpperInvariant();
        Color = color;
        SetCreatedBy(createdUserId);
    }

    public void Update(string brand, string model, int year, string licensePlate, string? color, Guid? updatedUserId = null)
    {
        Brand = brand;
        Model = model;
        Year = year;
        LicensePlate = StripDashes(licensePlate).ToUpperInvariant();
        Color = color;
        SetUpdatedBy(updatedUserId);
    }

    private static string StripDashes(string value) =>
        new(value.Where(c => c != '-').ToArray());
}
