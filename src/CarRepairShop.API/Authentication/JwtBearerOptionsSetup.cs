using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CarRepairShop.API.Authentication;

public class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IConfiguration _configuration;
    private readonly IJwtSigningKeyProvider _jwtSigningKeyProvider;

    public JwtBearerOptionsSetup(IConfiguration configuration, IJwtSigningKeyProvider jwtSigningKeyProvider)
    {
        _configuration = configuration;
        _jwtSigningKeyProvider = jwtSigningKeyProvider;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = _jwtSigningKeyProvider.GetSigningKeyAsync().GetAwaiter().GetResult();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "car-repair-auth",
            ValidAudience = jwtSettings["Audience"] ?? "car-repair-shop",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = "role",
            NameClaimType = "name",
            ClockSkew = TimeSpan.Zero
        };
    }

    public void Configure(JwtBearerOptions options)
        => Configure(Options.DefaultName, options);
}
