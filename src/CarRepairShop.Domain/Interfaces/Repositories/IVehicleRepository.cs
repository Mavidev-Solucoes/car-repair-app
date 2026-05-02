using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<Vehicle?> GetByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default);
    Task<bool> HasServiceOrdersAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<Vehicle, bool>>>? filters = null,
        CancellationToken cancellationToken = default);
}
