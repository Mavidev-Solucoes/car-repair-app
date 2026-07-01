using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class RequestApprovalCommandHandler : IRequestHandler<RequestApprovalCommand, ServiceOrderDto>
{
    private readonly Services.IServiceOrderApprovalRequestService _serviceOrderApprovalRequestService;

    public RequestApprovalCommandHandler(Services.IServiceOrderApprovalRequestService serviceOrderApprovalRequestService)
    {
        _serviceOrderApprovalRequestService = serviceOrderApprovalRequestService;
    }

    public Task<ServiceOrderDto> Handle(RequestApprovalCommand request, CancellationToken cancellationToken)
    {
        return _serviceOrderApprovalRequestService.RequestApprovalAsync(request, cancellationToken);
    }
}
