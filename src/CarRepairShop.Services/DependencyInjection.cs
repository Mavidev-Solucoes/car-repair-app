using CarRepairShop.Domain.Interfaces.Services;
using CarRepairShop.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace CarRepairShop.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        return services;
    }
}
