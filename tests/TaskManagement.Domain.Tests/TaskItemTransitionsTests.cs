using System;
using FluentAssertions;
using Xunit;
using TStatus = TaskManagement.Domain.Enums.TaskStatus;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Policies;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.Tests;

public class TaskItemTransitionsTests
{
    [Fact]
    public void Complete_WithExplicitUtcCompletedAt_SucceedsAndSetsProvidedValue()
    {
        var t = TaskItem.Create(Guid.NewGuid(), "t", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        var policy = new StatusTransitionPolicy();
        var ts = DateTime.UtcNow.AddMinutes(-5);

        t.ChangeStatus(TStatus.Completed, ts, policy);

        t.Status.Should().Be(TStatus.Completed);
        t.CompletedAt.Should().Be(ts);
    }

    [Fact]
    public void Pending_AllowedTransitions()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var t = TaskItem.Create(Guid.NewGuid(), "t", null, id1, id2, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        var policy = new StatusTransitionPolicy();

        t.ChangeStatus(TStatus.InProgress, null, policy);
        t.Status.Should().Be(TStatus.InProgress);

        t.ChangeStatus(TStatus.Pending, null, policy);
        t.Status.Should().Be(TStatus.Pending);

        t.ChangeStatus(TStatus.Completed, DateTime.UtcNow, policy);
        t.Status.Should().Be(TStatus.Completed);
    }

    [Fact]
    public void InProgress_AllowedTransitions()
    {
        var t = TaskItem.Create(Guid.NewGuid(), "t", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        var policy = new StatusTransitionPolicy();

        t.ChangeStatus(TStatus.InProgress, null, policy);
        t.Status.Should().Be(TStatus.InProgress);

        t.ChangeStatus(TStatus.Pending, null, policy);
        t.Status.Should().Be(TStatus.Pending);

        t.ChangeStatus(TStatus.InProgress, null, policy);
        t.ChangeStatus(TStatus.Completed, DateTime.UtcNow, policy);
        t.Status.Should().Be(TStatus.Completed);
    }

    [Fact]
    public void Cancelled_IsTerminal()
    {
        var t = TaskItem.Create(Guid.NewGuid(), "t", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        var policy = new StatusTransitionPolicy();

        t.ChangeStatus(TStatus.Cancelled, null, policy);

        var act = () => t.ChangeStatus(TStatus.Pending, null, policy);
        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Completed_IsTerminal()
    {
        var t = TaskItem.Create(Guid.NewGuid(), "t", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        var policy = new StatusTransitionPolicy();

        t.ChangeStatus(TStatus.Completed, DateTime.UtcNow, policy);

        var act = () => t.ChangeStatus(TStatus.InProgress, null, policy);
        act.Should().Throw<BusinessRuleViolationException>();
    }
}
