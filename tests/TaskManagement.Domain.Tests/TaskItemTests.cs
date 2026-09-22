using System;
using FluentAssertions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Policies;
using Xunit;
using TStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Tests;

public class TaskItemTests
{
    [Fact]
    public void Create_ValidTask_Succeeds()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Title", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        task.Status.Should().Be(TStatus.Pending);
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        var act = () => TaskItem.Create(Guid.NewGuid(), string.Empty, null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Create_WhitespaceTitle_Throws()
    {
        var act = () => TaskItem.Create(Guid.NewGuid(), "   ", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Create_DueBeforeStart_Throws()
    {
        var plannedStart = DateTime.UtcNow.AddDays(2);
        var due = DateTime.UtcNow;

        var act = () => TaskItem.Create(Guid.NewGuid(), "Task", null, Guid.NewGuid(), Guid.NewGuid(), plannedStart, due);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Create_DueEqualsStart_Succeeds()
    {
        var date = DateTime.UtcNow;

        var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Guid.NewGuid(), Guid.NewGuid(), date, date);

        task.DueAt.Should().Be(date);
        task.PlannedStartAt.Should().Be(date);
    }

    [Fact]
    public void Create_CreatorEqualsAssignee_Throws()
    {
        var employeeId = Guid.NewGuid();

        var act = () => TaskItem.Create(Guid.NewGuid(), "Task", null, employeeId, employeeId, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Complete_WithoutCompletedAt_SetsNow()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.Completed, null, policy);

        task.Status.Should().Be(TStatus.Completed);
        task.CompletedAt.Should().NotBeNull();
        task.CompletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Complete_WithUtcCompletedAt_UsesProvidedValue()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();
        var completedAt = DateTime.UtcNow.AddMinutes(-5);

        task.ChangeStatus(TStatus.Completed, completedAt, policy);

        task.Status.Should().Be(TStatus.Completed);
        task.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_WithNonUtcCompletedAt_Throws()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        var completedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        var act = () => task.ChangeStatus(TStatus.Completed, completedAt, policy);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Theory]
    [InlineData(TStatus.Pending)]
    [InlineData(TStatus.InProgress)]
    [InlineData(TStatus.Cancelled)]
    public void NonCompleted_Status_HasNullCompletedAt(TStatus status)
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        if (status != TStatus.Pending)
            task.ChangeStatus(status, null, policy);

        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void NonCompleted_Status_WithCompletedAt_Throws()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        var act = () => task.ChangeStatus(TStatus.InProgress, DateTime.UtcNow, policy);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Pending_To_InProgress_IsAllowed()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.InProgress, null, policy);

        task.Status.Should().Be(TStatus.InProgress);
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Pending_To_Completed_IsAllowed()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.Completed, null, policy);

        task.Status.Should().Be(TStatus.Completed);
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Pending_To_Cancelled_IsAllowed()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.Cancelled, null, policy);

        task.Status.Should().Be(TStatus.Cancelled);
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void InProgress_To_Pending_IsAllowed()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.InProgress, null, policy);
        task.ChangeStatus(TStatus.Pending, null, policy);

        task.Status.Should().Be(TStatus.Pending);
    }

    [Fact]
    public void InProgress_To_Completed_IsAllowed()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.InProgress, null, policy);
        task.ChangeStatus(TStatus.Completed, null, policy);

        task.Status.Should().Be(TStatus.Completed);
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void InProgress_To_Cancelled_IsAllowed()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.InProgress, null, policy);
        task.ChangeStatus(TStatus.Cancelled, null, policy);

        task.Status.Should().Be(TStatus.Cancelled);
    }

    [Theory]
    [InlineData(TStatus.Pending)]
    [InlineData(TStatus.InProgress)]
    [InlineData(TStatus.Cancelled)]
    public void Completed_Cannot_Transition_To_Another_Status(TStatus targetStatus)
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.Completed, DateTime.UtcNow, policy);

        var act = () => task.ChangeStatus(targetStatus, null, policy);

        act.Should().Throw<BusinessRuleViolationException>();

        task.Status.Should().Be(TStatus.Completed);
        task.CompletedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(TStatus.Pending)]
    [InlineData(TStatus.InProgress)]
    [InlineData(TStatus.Completed)]
    public void Cancelled_Cannot_Transition_To_Another_Status(TStatus targetStatus)
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.Cancelled, null, policy);

        var act = () => task.ChangeStatus(targetStatus, null, policy);

        act.Should().Throw<BusinessRuleViolationException>();

        task.Status.Should().Be(TStatus.Cancelled);
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Completed_Cannot_Transition_To_Completed()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.Completed, DateTime.UtcNow, policy);

        var act = () => task.ChangeStatus(TStatus.Completed, null, policy);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Cancelled_Cannot_Transition_To_Cancelled()
    {
        var task = CreateTask();
        var policy = new StatusTransitionPolicy();

        task.ChangeStatus(TStatus.Cancelled, null, policy);

        var act = () => task.ChangeStatus(TStatus.Cancelled, null, policy);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    private static TaskItem CreateTask() =>
        TaskItem.Create(Guid.NewGuid(), "Test task", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
}
