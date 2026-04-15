using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;

namespace CarRepairShop.Repository.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CarRepairShopDbContext _context;

    public UnitOfWork(CarRepairShopDbContext context)
    {
        _context = context;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
