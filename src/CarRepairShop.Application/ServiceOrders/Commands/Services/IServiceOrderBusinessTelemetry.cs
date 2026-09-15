using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Application.ServiceOrders.Commands.Services;

public interface IServiceOrderBusinessTelemetry
{
    Task RecordCreatedAsync(ServiceOrder order, CancellationToken cancellationToken);
    Task RecordStatusChangesAsync(ServiceOrder order, int previousHistoryCount, CancellationToken cancellationToken);
}
