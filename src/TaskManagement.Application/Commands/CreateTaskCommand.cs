namespace TaskManagement.Application.Commands;

public sealed class CreateTaskCommand
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public Guid CreatedByEmployeeId { get; init; }
    public Guid AssigneeEmployeeId { get; init; }
    public DateTime PlannedStartAt { get; init; }
    public DateTime DueAt { get; init; }
}
