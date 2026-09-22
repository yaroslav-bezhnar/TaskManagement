using TStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Policies;

public sealed class StatusTransitionPolicy
{
    private static readonly ISet<(TStatus From, TStatus To)> _allowed = new HashSet<(TStatus, TStatus)>
    {
        (TStatus.Pending, TStatus.InProgress),
        (TStatus.Pending, TStatus.Completed),
        (TStatus.Pending, TStatus.Cancelled),
        (TStatus.InProgress, TStatus.Pending),
        (TStatus.InProgress, TStatus.Completed),
        (TStatus.InProgress, TStatus.Cancelled)
    };

    public bool CanTransition(TStatus from, TStatus to) => from is not (TStatus.Completed or TStatus.Cancelled) && _allowed.Contains((from, to));
}
