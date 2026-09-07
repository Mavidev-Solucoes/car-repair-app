namespace CarRepairShop.Domain.Interfaces.Services;

public interface IJwtService
{
    Task<string> GenerateTokenAsync(string cpf, CancellationToken cancellationToken = default);
}
