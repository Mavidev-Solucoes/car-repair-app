using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Services;

public interface IEmailTemplateService
{
    Task<string> RenderServiceReceivedAsync(ServiceOrder order, Customer customer, Vehicle vehicle, User assignedEmployee);
    Task<string> RenderWaitingForApprovalAsync(ServiceOrder order, Customer customer, string approvalUrl, string rejectionUrl);
    Task<string> RenderServiceFinishedAsync(ServiceOrder order, Customer customer);
}
