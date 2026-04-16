using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Document == document, cancellationToken);
    }

    public async Task<bool> ExistsByDocumentAsync(string document, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c => c.Document == document, cancellationToken);
    }

    public async Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
