using System.Linq.Expressions;
using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByDocumentAsync(string personalId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByDocumentAsync(string personalId, CancellationToken cancellationToken = default);
    Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<Customer, bool>>>? filters = null,
        CancellationToken cancellationToken = default);
}
