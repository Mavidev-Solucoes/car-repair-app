using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<Customer?> GetByDocumentAsync(string personalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.PersonalId == personalId, cancellationToken);
    }

    public async Task<bool> ExistsByDocumentAsync(string personalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c => c.PersonalId == personalId, cancellationToken);
    }

    public async Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> HasVehiclesAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles.AnyAsync(vehicle => vehicle.CustomerId == customerId, cancellationToken);
    }

    public async Task<bool> HasServiceOrdersAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceOrders.AnyAsync(order => order.CustomerId == customerId, cancellationToken);
    }

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<Customer, bool>>>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (filters != null)
            foreach (var filter in filters)
                query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        query = orderBy?.ToLowerInvariant() switch
        {
            "name" => orderDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "email" => orderDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "createdat" => orderDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            "personalid" => orderDescending ? query.OrderByDescending(c => c.PersonalId) : query.OrderBy(c => c.PersonalId),
            _ => query.OrderBy(c => c.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
