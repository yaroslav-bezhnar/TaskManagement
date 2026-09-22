using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
