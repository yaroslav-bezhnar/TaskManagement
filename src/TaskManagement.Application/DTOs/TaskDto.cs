using TStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs;

public sealed class TaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public Guid CreatedByEmployeeId { get; init; }
    public Guid AssigneeEmployeeId { get; init; }
    public DateTime PlannedStartAt { get; init; }
    public DateTime DueAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
