using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;

namespace CarRepairShop.Repository.Repositories;

public class ServiceOrderItemRepository : Repository<ServiceOrderItem>, IServiceOrderItemRepository
{
    public ServiceOrderItemRepository(CarRepairShopDbContext context) : base(context) { }
}
