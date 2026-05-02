using CarRepairShop.Domain.Enums;

namespace CarRepairShop.Domain.Entities;

public class Employee : User
{
    private Employee() { }

    public Employee(string name, string email, string passwordHash, UserRole role)
        : base(name, email, passwordHash, role)
    {
        EnsureEmployeeRole(role);
    }

    public void Update(string name, string email, UserRole role)
    {
        EnsureEmployeeRole(role);
        UpdateCore(name, email, role);
    }

    private static void EnsureEmployeeRole(UserRole role)
    {
        if (role == UserRole.Customer)
            throw new InvalidOperationException("Customer role is not valid for employee accounts.");
    }
}
