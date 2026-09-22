using TStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Commands;

public sealed class ChangeTaskStatusCommand
{
    public Guid TaskId { get; init; }
    public TStatus TargetStatus { get; init; }
    public DateTime? CompletedAt { get; init; }
}
