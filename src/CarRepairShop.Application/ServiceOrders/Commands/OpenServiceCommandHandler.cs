using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class OpenServiceCommandHandler : IRequestHandler<OpenServiceCommand, ServiceOrderDto>
{
    private readonly Services.IServiceOrderOpeningService _serviceOrderOpeningService;

    public OpenServiceCommandHandler(Services.IServiceOrderOpeningService serviceOrderOpeningService)
    {
        _serviceOrderOpeningService = serviceOrderOpeningService;
    }

    public Task<ServiceOrderDto> Handle(OpenServiceCommand request, CancellationToken cancellationToken)
    {
        return _serviceOrderOpeningService.OpenAsync(request, cancellationToken);
    }
}
