using TaskManagement.Domain.Entities;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskDto>> GetByAssigneeAsync(Guid assigneeId, CancellationToken cancellationToken = default);
    Task ReloadAsync(TaskItem task, CancellationToken cancellationToken = default);
}
