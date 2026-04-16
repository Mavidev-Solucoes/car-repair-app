using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface IServiceOrderRepository : IRepository<ServiceOrder>
{
    Task<ServiceOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceOrder>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceOrder>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
}
