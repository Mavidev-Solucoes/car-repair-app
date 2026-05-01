namespace CarRepairShop.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string PersonalId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telephone { get; private set; } = string.Empty;

    private readonly List<Vehicle> _vehicles = new();
    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();

    private Customer() { }

    public Customer(string name, string personalId, string email, string telephone, Guid? createdUserId = null)
    {
        Name = name;
        PersonalId = StripToDigits(personalId);
        Email = email;
        Telephone = StripToDigits(telephone);
        SetCreatedBy(createdUserId);
    }

    public void Update(string name, string email, string telephone, Guid? updatedUserId = null)
    {
        Name = name;
        Email = email;
        Telephone = StripToDigits(telephone);
        SetUpdatedBy(updatedUserId);
    }

    private static string StripToDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
