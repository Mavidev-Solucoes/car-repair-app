namespace CarRepairShop.API.Authentication;

public interface IJwtSigningKeyProvider
{
    Task<string> GetSigningKeyAsync(CancellationToken cancellationToken = default);
}
