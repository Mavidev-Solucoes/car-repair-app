using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public abstract class User : BaseEntity
{
    public string Name { get; protected set; } = string.Empty;
    public string Email { get; protected set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; protected set; }
    public bool IsActive { get; private set; }

    protected User() { }

    protected User(string name, string email, string passwordHash, UserRole role)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    protected void UpdateCore(string name, string email, UserRole role)
    {
        Name = name;
        Email = email;
        Role = role;
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
