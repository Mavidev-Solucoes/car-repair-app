using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class ServiceOrderRepository : Repository<ServiceOrder>, IServiceOrderRepository
{
    public ServiceOrderRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<ServiceOrder?> GetWithAllDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(so => so.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(so => so.Customer)
            .Include(so => so.AssignedUser)
            .Include(so => so.ServiceItems)
            .Include(so => so.ServiceJobs)
                .ThenInclude(sj => sj.StatusHistory.OrderBy(h => h.ChangedAt))
            .Include(so => so.ServiceJobs)
                .ThenInclude(sj => sj.AssignedUser)
            .Include(so => so.StatusHistory.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ServiceOrder>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(so => so.Vehicle)
            .Include(so => so.Customer)
            .Include(so => so.AssignedUser)
            .Include(so => so.ServiceItems)
            .Include(so => so.ServiceJobs)
                .ThenInclude(sj => sj.AssignedUser)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ServiceStatusHistory>> GetStatusHistoryAsync(Guid serviceOrderId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceStatusHistory
            .Where(h => h.ServiceOrderId == serviceOrderId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }
}
