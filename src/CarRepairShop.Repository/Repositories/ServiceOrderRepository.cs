using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class ServiceOrderRepository : Repository<ServiceOrder>, IServiceOrderRepository
{
    public ServiceOrderRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<ServiceOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(so => so.ServiceItems)
            .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ServiceOrder>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(so => so.ServiceItems)
            .Where(so => so.VehicleId == vehicleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ServiceOrder>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(so => so.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(so => so.ServiceItems)
            .ToListAsync(cancellationToken);
    }
}
