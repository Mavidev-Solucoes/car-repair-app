using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Repositories;

public class VehicleRepository : Repository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(CarRepairShopDbContext context) : base(context) { }

    public async Task<Vehicle?> GetByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(v => v.LicensePlate == licensePlate, cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.CustomerId == customerId).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(v => v.LicensePlate == licensePlate, cancellationToken);
    }

    public async Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<Vehicle, bool>>>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (filters != null)
            foreach (var filter in filters)
                query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        query = orderBy?.ToLowerInvariant() switch
        {
            "brand" => orderDescending ? query.OrderByDescending(v => v.Brand) : query.OrderBy(v => v.Brand),
            "model" => orderDescending ? query.OrderByDescending(v => v.Model) : query.OrderBy(v => v.Model),
            "year" => orderDescending ? query.OrderByDescending(v => v.Year) : query.OrderBy(v => v.Year),
            "licenseplate" => orderDescending ? query.OrderByDescending(v => v.LicensePlate) : query.OrderBy(v => v.LicensePlate),
            "createdat" => orderDescending ? query.OrderByDescending(v => v.CreatedAt) : query.OrderBy(v => v.CreatedAt),
            _ => query.OrderBy(v => v.Brand)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
