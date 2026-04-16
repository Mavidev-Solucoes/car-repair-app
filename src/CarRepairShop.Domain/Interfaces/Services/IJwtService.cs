using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
