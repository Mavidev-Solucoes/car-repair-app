using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserType UserType { get; private set; }
    public bool IsActive { get; private set; }

    private User() { }

    public User(string name, string email, string passwordHash, UserRole role, UserType userType = UserType.Employee)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        UserType = userType;
        IsActive = true;
    }

    public void Update(string name, string email, UserRole role, UserType userType)
    {
        Name = name;
        Email = email;
        Role = role;
        UserType = userType;
        SetUpdatedAt();
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }
}
