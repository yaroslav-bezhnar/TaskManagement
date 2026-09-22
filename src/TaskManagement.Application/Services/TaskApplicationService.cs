using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Commands;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Policies;

namespace TaskManagement.Application.Services;

public class TaskApplicationService(IEmployeeRepository employeesRepository, ITaskRepository taskRepository, StatusTransitionPolicy policy)
{
    private const int MaxConcurrencyAttempts = 3;

    public async Task<TaskDto> CreateAsync(CreateTaskCommand cmd, CancellationToken cancellationToken = default)
    {
        TaskItem? added = null;

        await taskRepository.ExecuteInTransactionAsync(async () =>
        {
            var creator = await employeesRepository.GetByIdAsync(cmd.CreatedByEmployeeId, cancellationToken);

            if (creator == null) throw new NotFoundException("Creator employee not found");

            var assignee = await employeesRepository.GetByIdForUpdateAsync(cmd.AssigneeEmployeeId, cancellationToken);

            if (assignee == null) throw new NotFoundException("Assignee employee not found");
            if (!assignee.IsActive) throw new AppException("Assignee is inactive");
            if (creator.Id == assignee.Id) throw new AppException("Creator and assignee must be different");

            var task = TaskItem.Create(Guid.NewGuid(), cmd.Title, cmd.Description, cmd.CreatedByEmployeeId, cmd.AssigneeEmployeeId, cmd.PlannedStartAt, cmd.DueAt);
            await taskRepository.AddAsync(task, cancellationToken);
            added = task;
        }, cancellationToken);

        return ToDto(added!);
    }

    public async Task<TaskDto> ChangeStatusAsync(ChangeTaskStatusCommand cmd, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(cmd.TaskId, cancellationToken);

        if (task is null) throw new NotFoundException("Task not found");

        task.ChangeStatus(cmd.TargetStatus, cmd.CompletedAt, policy);

        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            try
            {
                await taskRepository.SaveChangesAsync(cancellationToken);

                return ToDto(task);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxConcurrencyAttempts)
            {
                await Task.Delay(BackoffDelay(attempt), cancellationToken);

                await taskRepository.ReloadAsync(task, cancellationToken);
                task.ChangeStatus(cmd.TargetStatus, cmd.CompletedAt, policy);
            }
        }

        throw new ConcurrencyConflictException($"Task '{cmd.TaskId}' was modified by another user after {MaxConcurrencyAttempts} attempts.");
    }

    public async Task<IReadOnlyList<TaskDto>> GetByAssigneeAsync(Guid assigneeId, CancellationToken cancellationToken = default) =>
        await taskRepository.GetByAssigneeAsync(assigneeId, cancellationToken);

    private static int BackoffDelay(int attempt)
    {
        var baseDelay = 50 * (int) Math.Pow(2, attempt - 1);

        return baseDelay + Random.Shared.Next(0, 30);
    }

    private static TaskDto ToDto(TaskItem item) =>
        new()
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            CreatedByEmployeeId = item.CreatedByEmployeeId,
            AssigneeEmployeeId = item.AssigneeEmployeeId,
            PlannedStartAt = item.PlannedStartAt,
            DueAt = item.DueAt,
            CompletedAt = item.CompletedAt,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
}
