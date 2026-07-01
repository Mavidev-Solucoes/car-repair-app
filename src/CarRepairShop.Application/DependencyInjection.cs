using System.Diagnostics.CodeAnalysis;
using CarRepairShop.Application.Common.Behaviors;
using CarRepairShop.Application.Settings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarRepairShop.Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<CarRepairShop.Application.ServiceOrders.Commands.Services.IServiceOrderNotificationService, CarRepairShop.Application.ServiceOrders.Commands.Services.ServiceOrderNotificationService>();
        services.AddScoped<CarRepairShop.Application.ServiceOrders.Commands.Services.IServiceOrderOpeningService, CarRepairShop.Application.ServiceOrders.Commands.Services.ServiceOrderOpeningService>();
        services.AddScoped<CarRepairShop.Application.ServiceOrders.Commands.Services.IServiceOrderApprovalRequestService, CarRepairShop.Application.ServiceOrders.Commands.Services.ServiceOrderApprovalRequestService>();

        return services;
    }
}
