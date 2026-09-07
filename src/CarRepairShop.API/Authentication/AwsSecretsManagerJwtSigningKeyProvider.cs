using System.Text.Json;
using System.Text;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace CarRepairShop.API.Authentication;

public class AwsSecretsManagerJwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private static readonly string[] JsonSecretKeyCandidates =
    [
        "secretKey",
        "SecretKey",
        "jwtSecretKey",
        "JwtSecretKey",
        "JWT_SECRET_KEY",
        "JwtSettings:SecretKey"
    ];

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public AwsSecretsManagerJwtSigningKeyProvider(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<string> GetSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        var localSecretKey = _configuration["JwtSettings:SecretKey"];
        var isDevelopment = _environment.IsDevelopment();

        if (isDevelopment && IsValidSigningKey(localSecretKey))
        {
            return localSecretKey!;
        }

        var secretName = _configuration["JwtSecretProvider:SecretName"];
        if (string.IsNullOrWhiteSpace(secretName))
        {
            if (isDevelopment && IsValidSigningKey(localSecretKey))
            {
                return localSecretKey!;
            }

            throw new InvalidOperationException("JwtSecretProvider:SecretName is not configured.");
        }

        var signingKey = await GetSigningKeyFromSecretsManagerAsync(secretName, cancellationToken);
        if (IsValidSigningKey(signingKey))
        {
            return signingKey;
        }

        if (isDevelopment && IsValidSigningKey(localSecretKey))
        {
            return localSecretKey!;
        }

        throw new InvalidOperationException("Unable to resolve a valid JWT signing key.");
    }

    private async Task<string?> GetSigningKeyFromSecretsManagerAsync(string secretName, CancellationToken cancellationToken)
    {
        var regionName = _configuration["JwtSecretProvider:Region"];
        using var client = CreateSecretsManagerClient(regionName);
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = secretName
        }, cancellationToken);

        var rawSecret = response.SecretString;
        if (string.IsNullOrWhiteSpace(rawSecret) && response.SecretBinary is not null)
        {
            using var reader = new StreamReader(response.SecretBinary, Encoding.UTF8);
            rawSecret = await reader.ReadToEndAsync(cancellationToken);
        }

        return ExtractSigningKey(rawSecret);
    }

    private static IAmazonSecretsManager CreateSecretsManagerClient(string? regionName)
    {
        if (!string.IsNullOrWhiteSpace(regionName))
        {
            return new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(regionName));
        }

        return new AmazonSecretsManagerClient();
    }

    private static string? ExtractSigningKey(string? secretString)
    {
        if (string.IsNullOrWhiteSpace(secretString))
        {
            return null;
        }

        var trimmedSecret = secretString.Trim();
        if (!trimmedSecret.StartsWith('{'))
        {
            return trimmedSecret;
        }

        using var document = JsonDocument.Parse(trimmedSecret);
        foreach (var keyCandidate in JsonSecretKeyCandidates)
        {
            if (document.RootElement.TryGetProperty(keyCandidate, out var keyElement))
            {
                var keyValue = keyElement.GetString();
                if (!string.IsNullOrWhiteSpace(keyValue))
                {
                    return keyValue.Trim();
                }
            }
        }

        return null;
    }

    private static bool IsValidSigningKey(string? signingKey)
        => !string.IsNullOrWhiteSpace(signingKey) && signingKey.Trim().Length >= 32;
}
