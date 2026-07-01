using CarRepairShop.Application.DTOs;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public interface IServiceOrderOpeningService
{
    Task<ServiceOrderDto> OpenAsync(OpenServiceCommand request, CancellationToken cancellationToken);
}
