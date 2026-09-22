using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskManagement.Application.Commands;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Exceptions;
using Xunit;
using TStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Tests;

public class TaskApplicationServiceAdditionalTests
{
    [Fact]
    public async Task CreateTask_WithValidCreatorAndActiveAssignee_Succeeds()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        var creator = Employee.Create(Guid.NewGuid(), "a", "b", "a@x.com");
        var assignee = Employee.Create(Guid.NewGuid(), "c", "d", "c@x.com");
        empRepo.Setup(r => r.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);
        empRepo.Setup(r => r.GetByIdAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        empRepo.Setup(r => r.GetByIdForUpdateAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);

        TaskItem captured = null!;
        taskRepo.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);
        taskRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        taskRepo.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, ct) => Task.Run(async () => { await op(); await taskRepo.Object.SaveChangesAsync(ct); }, ct));

        var svc = new TaskApplicationService(empRepo.Object, taskRepo.Object, new Domain.Policies.StatusTransitionPolicy());
        var cmd = new CreateTaskCommand { Title = "t", CreatedByEmployeeId = creator.Id, AssigneeEmployeeId = assignee.Id, PlannedStartAt = DateTime.UtcNow, DueAt = DateTime.UtcNow.AddDays(1) };

        var dto = await svc.CreateAsync(cmd);

        dto.Title.Should().Be("t");
        captured.Should().NotBeNull();
        captured.AssigneeEmployeeId.Should().Be(assignee.Id);
    }

    [Fact]
    public async Task CreateTask_MissingCreator_Throws()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        empRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?) null);
        taskRepo.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var svc = new TaskApplicationService(empRepo.Object, taskRepo.Object, new Domain.Policies.StatusTransitionPolicy());
        var cmd = new CreateTaskCommand { Title = "t", CreatedByEmployeeId = Guid.NewGuid(), AssigneeEmployeeId = Guid.NewGuid(), PlannedStartAt = DateTime.UtcNow, DueAt = DateTime.UtcNow.AddDays(1) };

        await svc.Invoking(s => s.CreateAsync(cmd)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateTask_MissingAssignee_Throws()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        var creator = Employee.Create(Guid.NewGuid(), "a", "b", "a@x.com");
        empRepo.Setup(r => r.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);
        empRepo.Setup(r => r.GetByIdAsync(It.Is<Guid>(g => g != creator.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?) null);
        taskRepo.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var svc = new TaskApplicationService(empRepo.Object, taskRepo.Object, new Domain.Policies.StatusTransitionPolicy());
        var cmd = new CreateTaskCommand { Title = "t", CreatedByEmployeeId = creator.Id, AssigneeEmployeeId = Guid.NewGuid(), PlannedStartAt = DateTime.UtcNow, DueAt = DateTime.UtcNow.AddDays(1) };

        await svc.Invoking(s => s.CreateAsync(cmd)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateTask_InvalidDates_Throws()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        var creator = Employee.Create(Guid.NewGuid(), "a", "b", "a@x.com");
        var assignee = Employee.Create(Guid.NewGuid(), "c", "d", "c@x.com");
        empRepo.Setup(r => r.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);
        empRepo.Setup(r => r.GetByIdAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        empRepo.Setup(r => r.GetByIdForUpdateAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        taskRepo.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var svc = new TaskApplicationService(empRepo.Object, taskRepo.Object, new Domain.Policies.StatusTransitionPolicy());
        var cmd = new CreateTaskCommand { Title = "t", CreatedByEmployeeId = creator.Id, AssigneeEmployeeId = assignee.Id, PlannedStartAt = DateTime.UtcNow.AddDays(2), DueAt = DateTime.UtcNow };

        await svc.Invoking(s => s.CreateAsync(cmd)).Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task ChangeStatus_Success_ReturnsUpdatedTask()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        var t = TaskItem.Create(Guid.NewGuid(), "t", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        taskRepo.Setup(r => r.GetByIdAsync(t.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(t);
        taskRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = new TaskApplicationService(empRepo.Object, taskRepo.Object, new Domain.Policies.StatusTransitionPolicy());
        var cmd = new ChangeTaskStatusCommand { TaskId = t.Id, TargetStatus = TStatus.Completed };

        var dto = await svc.ChangeStatusAsync(cmd);

        dto.Status.Should().Be(TStatus.Completed);
        dto.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStatus_MissingTask_Throws()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();
        taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?) null);

        var svc = new TaskApplicationService(empRepo.Object, taskRepo.Object, new Domain.Policies.StatusTransitionPolicy());
        var cmd = new ChangeTaskStatusCommand { TaskId = Guid.NewGuid(), TargetStatus = TStatus.Completed };

        await svc.Invoking(s => s.ChangeStatusAsync(cmd)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Complete_WithoutCompletedAt_AssignsUtcTimestamp()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        var t = TaskItem.Create(Guid.NewGuid(), "t", null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        taskRepo.Setup(r => r.GetByIdAsync(t.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(t);
        taskRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = new TaskApplicationService(empRepo.Object, taskRepo.Object, new Domain.Policies.StatusTransitionPolicy());
        var cmd = new ChangeTaskStatusCommand { TaskId = t.Id, TargetStatus = TStatus.Completed };

        var dto = await svc.ChangeStatusAsync(cmd);

        dto.Status.Should().Be(TStatus.Completed);
        dto.CompletedAt.Should().NotBeNull();
        dto.CompletedAt.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetByAssignee_ForwardsRepositoryResults()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        var now = DateTime.UtcNow;
        var dtoList = new List<TaskDto>
        {
            new() { Id = Guid.NewGuid(), Title = "A", DueAt = now.AddDays(1), CreatedAt = now.AddHours(2) },
            new() { Id = Guid.NewGuid(), Title = "B", DueAt = now.AddDays(1), CreatedAt = now.AddHours(1) },
            new() { Id = Guid.NewGuid(), Title = "C", DueAt = now.AddDays(2), CreatedAt = now }
        };

        taskRepo.Setup(r => r.GetByAssigneeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtoList);

        var svc = new TaskApplicationService(empRepo.Object, taskRepo.Object, new Domain.Policies.StatusTransitionPolicy());
        var results = await svc.GetByAssigneeAsync(Guid.NewGuid());

        results.Should().HaveCount(3);
    }
}
