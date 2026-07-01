using CarRepairShop.Application.Settings;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public class ServiceOrderNotificationService : IServiceOrderNotificationService
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<ServiceOrderNotificationService> _logger;

    public ServiceOrderNotificationService(
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AppSettings> appSettings,
        ILogger<ServiceOrderNotificationService> logger)
    {
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task NotifyServiceReceivedAsync(ServiceOrder serviceOrder, Customer customer, Vehicle vehicle, Employee employee, CancellationToken cancellationToken)
    {
        try
        {
            var body = await _emailTemplateService.RenderServiceReceivedAsync(serviceOrder, customer, vehicle, employee);
            await _emailService.SendAsync(customer.Email, customer.Name,
                "Your service request has been received", body, isHtml: true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send service received notification for service order {ServiceOrderId}.",
                serviceOrder.Id);
        }
    }

    public async Task NotifyApprovalRequestedAsync(ServiceOrder serviceOrder, Customer customer, CancellationToken cancellationToken)
    {
        var baseUrl = _appSettings.BaseUrl.TrimEnd('/');
        var approvalUrl = $"{baseUrl}/api/services/{serviceOrder.Id}/approve";
        var rejectionUrl = $"{baseUrl}/api/services/{serviceOrder.Id}/reject";

        try
        {
            var body = await _emailTemplateService.RenderWaitingForApprovalAsync(serviceOrder, customer, approvalUrl, rejectionUrl);
            await _emailService.SendAsync(customer.Email, customer.Name,
                "Your service requires your approval", body, isHtml: true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send approval notification for service order {ServiceOrderId}.",
                serviceOrder.Id);
        }
    }
}
