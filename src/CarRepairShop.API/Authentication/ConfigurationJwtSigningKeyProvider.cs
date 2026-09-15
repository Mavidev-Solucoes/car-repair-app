namespace CarRepairShop.API.Authentication;

public class ConfigurationJwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private static readonly string[] ConfigurationKeys =
    [
        "JwtSettings:SecretKey",
        "JwtSecretKey",
        "JWT_SECRET_KEY"
    ];

    private readonly IConfiguration _configuration;

    public ConfigurationJwtSigningKeyProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<string> GetSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        foreach (var key in ConfigurationKeys)
        {
            var signingKey = _configuration[key];
            if (IsValidSigningKey(signingKey))
            {
                return Task.FromResult(signingKey!.Trim());
            }
        }

        throw new InvalidOperationException("JWT signing key is not configured or is shorter than 32 characters.");
    }

    private static bool IsValidSigningKey(string? signingKey)
        => !string.IsNullOrWhiteSpace(signingKey) && signingKey.Trim().Length >= 32;
}
