using System.Text;
using System.Text.Json;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace CarRepairShop.Services.Implementations;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public JwtService(IConfiguration configuration, HttpClient? httpClient = null)
    {
        _configuration = configuration;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<string> GenerateTokenAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var authLambdaSection = _configuration.GetSection("AuthLambda");
        var baseUrl = authLambdaSection["BaseUrl"]
            ?? throw new InvalidOperationException("AuthLambda BaseUrl is not configured.");
        var tokenPath = authLambdaSection["TokenPath"] ?? "/auth/token";
        var normalizedCpf = new string(cpf.Where(char.IsDigit).ToArray());

        var payload = JsonSerializer.Serialize(new { cpf = normalizedCpf });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        _httpClient.BaseAddress = new Uri(baseUrl);
        using var response = await _httpClient.PostAsync(tokenPath, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Lambda auth request failed with status code {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("accessToken", out var tokenElement))
        {
            throw new InvalidOperationException("Lambda auth response does not contain accessToken.");
        }

        var accessToken = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Lambda auth response returned an empty accessToken.");
        }

        return accessToken;
    }
}
