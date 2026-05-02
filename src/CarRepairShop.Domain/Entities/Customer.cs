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
        PersonalId = StripToDigits(personalId);
        Email = email;
        Telephone = StripToDigits(telephone);
        SetCreatedBy(createdUserId);
    }

    public void Update(string name, string email, string telephone, Guid? updatedUserId = null)
    {
        UpdateCore(name, email, Domain.Enums.UserRole.Customer);
        Telephone = StripToDigits(telephone);
        SetUpdatedBy(updatedUserId);
    }

    private static string StripToDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
