using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CarRepairShop.UnitTests.Services;

public class JwtServiceTests
{
    private const string SecretKey = "this-is-a-secret-key-for-unit-testing-1234567890";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";
    private const string ExpirationMinutes = "60";

    private static JwtService CreateService(
        string? secretKey = SecretKey,
        string? issuer = Issuer,
        string? audience = Audience,
        string? expirationMinutes = ExpirationMinutes)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = secretKey,
                ["JwtSettings:Issuer"] = issuer,
                ["JwtSettings:Audience"] = audience,
                ["JwtSettings:ExpirationMinutes"] = expirationMinutes,
            })
            .Build();

        return new JwtService(config);
    }

    private static User CreateUser() =>
        new("John Doe", "john@example.com", "hash", UserRole.Admin, UserType.Employee);

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ReturnsReadableJwtToken()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);

        Assert.True(new JwtSecurityTokenHandler().CanReadToken(token));
    }

    [Fact]
    public void GenerateToken_TokenContainsSubClaim()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(parsed.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_TokenContainsEmailClaim()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(parsed.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateToken_TokenContainsNameClaim()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(parsed.Claims, c => c.Type == JwtRegisteredClaimNames.Name && c.Value == user.Name);
    }

    [Fact]
    public void GenerateToken_TokenContainsRoleClaim()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(parsed.Claims, c => c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectIssuer()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Issuer, parsed.Issuer);
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectAudience()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(Audience, parsed.Audiences);
    }

    [Fact]
    public void GenerateToken_TokenIsValidatableWithCorrectKey()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParams, out _);

        Assert.NotNull(principal);
    }

    [Fact]
    public void GenerateToken_ThrowsWhenSecretKeyNotConfigured()
    {
        var service = CreateService(secretKey: null);
        var user = CreateUser();

        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(user));
    }

    [Fact]
    public void GenerateToken_UsesDefaultIssuerWhenNotConfigured()
    {
        var service = CreateService(issuer: null);
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("CarRepairShop", parsed.Issuer);
    }

    [Fact]
    public void GenerateToken_UsesDefaultAudienceWhenNotConfigured()
    {
        var service = CreateService(audience: null);
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains("CarRepairShop", parsed.Audiences);
    }

    [Fact]
    public void GenerateToken_UsesDefaultExpirationWhenNotConfigured()
    {
        var service = CreateService(expirationMinutes: null);
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(parsed.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_TokenContainsJtiClaim()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var jti = parsed.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.NotNull(jti);
        Assert.True(Guid.TryParse(jti.Value, out _));
    }

    [Fact]
    public void GenerateToken_EachTokenHasUniqueJti()
    {
        var service = CreateService();
        var user = CreateUser();

        var token1 = service.GenerateToken(user);
        var token2 = service.GenerateToken(user);

        var parsed1 = new JwtSecurityTokenHandler().ReadJwtToken(token1);
        var parsed2 = new JwtSecurityTokenHandler().ReadJwtToken(token2);

        var jti1 = parsed1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = parsed2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }
}
