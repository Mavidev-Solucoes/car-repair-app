using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Domain.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the <see cref="Employee"/> with the given id, or <c>null</c> if not found
    /// or the record belongs to a different discriminator (e.g. Customer).
    /// Avoids runtime downcasts in application-layer callers.
    /// </summary>
    Task<Employee?> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
