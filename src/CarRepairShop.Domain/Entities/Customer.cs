using CarRepairShop.Domain.ValueObjects;

namespace CarRepairShop.Domain.Entities;

public class Customer : User
{
    public string PersonalId { get; private set; } = string.Empty;
    public string Telephone { get; private set; } = string.Empty;

    private readonly List<Vehicle> _vehicles = new();
    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();

    private Customer() { }

    public Customer(
        string name,
        string personalId,
        string email,
        string telephone,
        string passwordHash,
        Guid? createdUserId = null)
        : base(name, email, passwordHash, Domain.Enums.UserRole.Customer)
    {
        Name = name;
        PersonalId = new PersonalId(personalId).Value;
        Email = email;
        Telephone = new PhoneNumber(telephone).Value;
        SetCreatedBy(createdUserId);
    }

    public void Update(string name, string email, string telephone, Guid? updatedUserId = null)
    {
        UpdateCore(name, email, Domain.Enums.UserRole.Customer);
        Telephone = new PhoneNumber(telephone).Value;
        SetUpdatedBy(updatedUserId);
    }
}
