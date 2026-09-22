using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Policies;
using TStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Entities;

public sealed class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public Guid CreatedByEmployeeId { get; private set; }
    public Guid AssigneeEmployeeId { get; private set; }
    public DateTime PlannedStartAt { get; private set; }
    public DateTime DueAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public TStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TaskItem() { }

    private TaskItem(Guid id, string title, string? description, Guid createdByEmployeeId, Guid assigneeEmployeeId, DateTime plannedStartAt, DateTime dueAt, DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        CreatedByEmployeeId = createdByEmployeeId;
        AssigneeEmployeeId = assigneeEmployeeId;
        PlannedStartAt = plannedStartAt;
        DueAt = dueAt;
        Status = TStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static TaskItem Create(Guid id, string title, string? description, Guid createdByEmployeeId, Guid assigneeEmployeeId, DateTime plannedStartAt, DateTime dueAt, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new BusinessRuleViolationException("Title is required");
        if (dueAt < plannedStartAt) throw new BusinessRuleViolationException("DueAt cannot be earlier than PlannedStartAt");
        if (createdByEmployeeId == assigneeEmployeeId) throw new BusinessRuleViolationException("Creator and assignee must be different employees");

        var utcNow = createdAt ?? DateTime.UtcNow;

        if (utcNow.Kind != DateTimeKind.Utc) throw new BusinessRuleViolationException("CreatedAt must be UTC");

        return new TaskItem(id, title, description, createdByEmployeeId, assigneeEmployeeId, plannedStartAt, dueAt, utcNow);
    }

    public void ChangeStatus(TStatus targetStatus, DateTime? completedAt, StatusTransitionPolicy policy)
    {
        if (!policy.CanTransition(Status, targetStatus))
            throw new BusinessRuleViolationException($"Invalid status transition from {Status} to {targetStatus}");

        if (targetStatus == TStatus.Completed)
        {
            var ts = completedAt ?? DateTime.UtcNow;

            if (ts.Kind != DateTimeKind.Utc) throw new BusinessRuleViolationException("CompletedAt must be UTC");

            CompletedAt = ts;
        }
        else
        {
            if (completedAt.HasValue) throw new BusinessRuleViolationException("CompletedAt must be null when status is not Completed");

            CompletedAt = null;
        }

        Status = targetStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
