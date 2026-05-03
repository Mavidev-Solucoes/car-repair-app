using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;

namespace CarRepairShop.Repository.Repositories;

public class ServiceStatusHistoryRepository : Repository<ServiceStatusHistory>, IServiceStatusHistoryRepository
{
    public ServiceStatusHistoryRepository(CarRepairShopDbContext context) : base(context) { }
}
