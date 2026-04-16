using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default);
    Task<bool> ExistsByDocumentAsync(string document, CancellationToken cancellationToken = default);
    Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken cancellationToken = default);
}
