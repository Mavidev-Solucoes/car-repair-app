using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface IServiceOrderRepository : IRepository<ServiceOrder>
{
    Task<ServiceOrder?> GetWithAllDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceOrder>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceStatusHistory>> GetStatusHistoryAsync(Guid serviceOrderId, CancellationToken cancellationToken = default);
}
