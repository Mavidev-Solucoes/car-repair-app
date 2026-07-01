using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public interface IServiceOrderNotificationService
{
    Task NotifyServiceReceivedAsync(ServiceOrder serviceOrder, Customer customer, Vehicle vehicle, Employee employee, CancellationToken cancellationToken);
    Task NotifyApprovalRequestedAsync(ServiceOrder serviceOrder, Customer customer, CancellationToken cancellationToken);
}
