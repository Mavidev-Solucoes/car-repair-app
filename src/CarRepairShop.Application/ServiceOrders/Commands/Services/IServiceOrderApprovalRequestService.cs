using CarRepairShop.Application.DTOs;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public interface IServiceOrderApprovalRequestService
{
    Task<ServiceOrderDto> RequestApprovalAsync(RequestApprovalCommand request, CancellationToken cancellationToken);
}
