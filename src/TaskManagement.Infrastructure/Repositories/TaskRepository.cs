using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Repositories;

internal class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;

    public TaskRepository(AppDbContext db) => _db = db;

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _db.Tasks.Add(task);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The record was modified by another user.", ex);
        }
    }

    public Task ReloadAsync(TaskItem task, CancellationToken cancellationToken = default) => _db.Entry(task).ReloadAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation();
            await SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);

            throw;
        }
    }

    public async Task<IReadOnlyList<TaskDto>> GetByAssigneeAsync(Guid assigneeId, CancellationToken cancellationToken = default)
    {
        var query = _db.Tasks.AsNoTracking()
            .Where(t => t.AssigneeEmployeeId == assigneeId)
            .OrderBy(t => t.DueAt)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedByEmployeeId = t.CreatedByEmployeeId,
                AssigneeEmployeeId = t.AssigneeEmployeeId,
                PlannedStartAt = t.PlannedStartAt,
                DueAt = t.DueAt,
                CompletedAt = t.CompletedAt,
                Status = t.Status,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            });

        return await query.ToListAsync(cancellationToken);
    }
}
