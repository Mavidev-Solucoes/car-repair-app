namespace CarRepairShop.Domain.Entities;

public class Vehicle : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string LicensePlate { get; private set; } = string.Empty;
    public string? Color { get; private set; }

    public Customer Customer { get; private set; } = null!;

    private readonly List<ServiceOrder> _serviceOrders = new();
    public IReadOnlyCollection<ServiceOrder> ServiceOrders => _serviceOrders.AsReadOnly();

    private Vehicle() { }

    public Vehicle(Guid customerId, string make, string model, int year, string licensePlate, string? color = null)
    {
        CustomerId = customerId;
        Make = make;
        Model = model;
        Year = year;
        LicensePlate = licensePlate;
        Color = color;
    }

    public void Update(string make, string model, int year, string licensePlate, string? color)
    {
        Make = make;
        Model = model;
        Year = year;
        LicensePlate = licensePlate;
        Color = color;
        SetUpdatedAt();
    }
}
