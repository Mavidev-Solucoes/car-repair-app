using System.Diagnostics.CodeAnalysis;
using CarRepairShop.Domain.Interfaces.Services;
using CarRepairShop.Services.Implementations;
using CarRepairShop.Services.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarRepairShop.Services;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddHttpClient<IJwtService, JwtService>();

        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        return services;
    }
}
