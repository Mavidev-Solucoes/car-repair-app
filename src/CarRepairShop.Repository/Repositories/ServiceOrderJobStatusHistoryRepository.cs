using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;

namespace CarRepairShop.Repository.Repositories;

public class ServiceOrderJobStatusHistoryRepository : Repository<ServiceOrderJobStatusHistory>, IServiceOrderJobStatusHistoryRepository
{
    public ServiceOrderJobStatusHistoryRepository(CarRepairShopDbContext context) : base(context) { }
}
