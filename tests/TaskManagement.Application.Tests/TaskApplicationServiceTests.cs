using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using TStatus = TaskManagement.Domain.Enums.TaskStatus;
using TaskManagement.Application.Commands;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Policies;

namespace TaskManagement.Application.Tests;

public class TaskApplicationServiceTests
{
    [Fact]
    public async Task CreateTask_WithInactiveAssignee_ThrowsAppException()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var creator = CreateEmployee(isActive: true);
        var assignee = CreateEmployee(isActive: false);

        employeeRepository
            .Setup(r => r.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);
        employeeRepository
            .Setup(r => r.GetByIdAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        employeeRepository
            .Setup(r => r.GetByIdForUpdateAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        taskRepository
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var service = CreateService(employeeRepository, taskRepository);
        var command = CreateCommand(creator.Id, assignee.Id);

        var act = () => service.CreateAsync(command);

        await act.Should().ThrowAsync<AppException>().WithMessage("Assignee is inactive");

        taskRepository.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
        taskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTask_WithMissingCreator_ThrowsNotFoundException()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var creatorId = Guid.NewGuid();
        var assignee = CreateEmployee(isActive: true);

        employeeRepository
            .Setup(r => r.GetByIdAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?) null);
        employeeRepository
            .Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?) null);
        taskRepository
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var service = CreateService(employeeRepository, taskRepository);
        var command = CreateCommand(creatorId, assignee.Id);

        var act = () => service.CreateAsync(command);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Creator employee not found");
    }

    [Fact]
    public async Task CreateTask_WithMissingAssignee_ThrowsNotFoundException()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var creator = CreateEmployee(isActive: true);
        var assigneeId = Guid.NewGuid();

        employeeRepository
            .Setup(r => r.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);
        employeeRepository
            .Setup(r => r.GetByIdAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?) null);
        employeeRepository
            .Setup(r => r.GetByIdForUpdateAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?) null);
        taskRepository
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var service = CreateService(employeeRepository, taskRepository);
        var command = CreateCommand(creator.Id, assigneeId);

        var act = () => service.CreateAsync(command);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Assignee employee not found");
    }

    [Fact]
    public async Task CreateTask_WithSameCreatorAndAssignee_ThrowsAppException()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var employee = CreateEmployee(isActive: true);

        employeeRepository
            .Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        employeeRepository
            .Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        taskRepository
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var service = CreateService(employeeRepository, taskRepository);
        var command = CreateCommand(employee.Id, employee.Id);

        var act = () => service.CreateAsync(command);

        await act.Should().ThrowAsync<AppException>().WithMessage("Creator and assignee must be different");

        taskRepository.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTask_WithValidData_CreatesAndSavesTask()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var creator = CreateEmployee(isActive: true);
        var assignee = CreateEmployee(isActive: true);

        employeeRepository
            .Setup(r => r.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);
        employeeRepository
            .Setup(r => r.GetByIdAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        employeeRepository
            .Setup(r => r.GetByIdForUpdateAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);

        TaskItem? addedTask = null;

        taskRepository
            .Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((task, _) => addedTask = task)
            .Returns(Task.CompletedTask);
        taskRepository
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var service = CreateService(employeeRepository, taskRepository);
        var command = CreateCommand(creator.Id, assignee.Id);

        var result = await service.CreateAsync(command);

        result.Should().NotBeNull();
        result.Title.Should().Be(command.Title);
        result.CreatedByEmployeeId.Should().Be(creator.Id);
        result.AssigneeEmployeeId.Should().Be(assignee.Id);
        result.Status.Should().Be(TStatus.Pending);

        addedTask.Should().NotBeNull();

        taskRepository.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
        taskRepository.Verify(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatus_TaskNotFound_ThrowsNotFoundException()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        taskRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?) null);

        var service = CreateService(employeeRepository, taskRepository);
        var command = new ChangeTaskStatusCommand
        {
            TaskId = Guid.NewGuid(),
            TargetStatus = TStatus.Completed
        };

        var act = () => service.ChangeStatusAsync(command);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Task not found");

        taskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_ToCompleted_UpdatesAndSavesTask()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var task = CreateTask();

        taskRepository
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService(employeeRepository, taskRepository);
        var command = new ChangeTaskStatusCommand
        {
            TaskId = task.Id,
            TargetStatus = TStatus.Completed
        };

        var result = await service.ChangeStatusAsync(command);

        result.Status.Should().Be(TStatus.Completed);
        result.CompletedAt.Should().NotBeNull();

        taskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatus_ToCancelled_UpdatesAndSavesTask()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var task = CreateTask();

        taskRepository
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService(employeeRepository, taskRepository);
        var command = new ChangeTaskStatusCommand
        {
            TaskId = task.Id,
            TargetStatus = TStatus.Cancelled
        };

        var result = await service.ChangeStatusAsync(command);

        result.Status.Should().Be(TStatus.Cancelled);
        result.CompletedAt.Should().BeNull();

        taskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatus_FromCompleted_ThrowsBusinessRuleViolation()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var task = CreateTask();
        task.ChangeStatus(TStatus.Completed, DateTime.UtcNow, new StatusTransitionPolicy());

        taskRepository
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService(employeeRepository, taskRepository);
        var command = new ChangeTaskStatusCommand
        {
            TaskId = task.Id,
            TargetStatus = TStatus.Pending
        };

        var act = () => service.ChangeStatusAsync(command);

        await act.Should().ThrowAsync<TaskManagement.Domain.Exceptions.BusinessRuleViolationException>();

        taskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_FromCancelled_ThrowsBusinessRuleViolation()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var task = CreateTask();
        task.ChangeStatus(TStatus.Cancelled, null, new StatusTransitionPolicy());

        taskRepository
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService(employeeRepository, taskRepository);
        var command = new ChangeTaskStatusCommand
        {
            TaskId = task.Id,
            TargetStatus = TStatus.Pending
        };

        var act = () => service.ChangeStatusAsync(command);

        await act.Should().ThrowAsync<TaskManagement.Domain.Exceptions.BusinessRuleViolationException>();

        taskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByAssignee_ReturnsProjectedTasks()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var assigneeId = Guid.NewGuid();

        var tasks = new List<TaskDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "A",
                DueAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "B",
                DueAt = DateTime.UtcNow.AddDays(2),
                CreatedAt = DateTime.UtcNow
            }
        };

        taskRepository
            .Setup(r => r.GetByAssigneeAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var service = CreateService(employeeRepository, taskRepository);

        var result = await service.GetByAssigneeAsync(assigneeId);

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("A");
        result[1].Title.Should().Be("B");

        taskRepository.Verify(r => r.GetByAssigneeAsync(assigneeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByAssignee_WhenNoTasks_ReturnsEmptyList()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        var taskRepository = new Mock<ITaskRepository>();

        var assigneeId = Guid.NewGuid();

        taskRepository
            .Setup(r => r.GetByAssigneeAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService(employeeRepository, taskRepository);

        var result = await service.GetByAssigneeAsync(assigneeId);

        result.Should().BeEmpty();
    }

    private static TaskApplicationService CreateService(Mock<IEmployeeRepository> employeeRepository, Mock<ITaskRepository> taskRepository) =>
        new(employeeRepository.Object, taskRepository.Object, new StatusTransitionPolicy());

    private static Employee CreateEmployee(bool isActive) =>
        Employee.Create(Guid.NewGuid(), "Test", "Employee", $"{Guid.NewGuid():N}@example.com", isActive);

    private static TaskItem CreateTask() =>
        TaskItem.Create(Guid.NewGuid(),
            "Test task",
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1));

    private static CreateTaskCommand CreateCommand(Guid creatorId, Guid assigneeId) =>
        new()
        {
            Title = "Test task",
            Description = "Test description",
            CreatedByEmployeeId = creatorId,
            AssigneeEmployeeId = assigneeId,
            PlannedStartAt = DateTime.UtcNow,
            DueAt = DateTime.UtcNow.AddDays(1)
        };

    [Fact]
    public async Task ChangeStatus_RetryOnConcurrency_SucceedsAfterRetry()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        var task = CreateTask();

        taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        taskRepo.Setup(r => r.ReloadAsync(task, It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => { typeof(TaskItem).GetProperty(nameof(TaskItem.Status))!.SetValue(t, TStatus.InProgress); })
            .Returns(Task.CompletedTask);

        taskRepo.SetupSequence(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"))
            .Returns(Task.CompletedTask);

        var svc = CreateService(empRepo, taskRepo);
        var cmd = new ChangeTaskStatusCommand
        {
            TaskId = task.Id,
            TargetStatus = TStatus.Completed
        };

        var dto = await svc.ChangeStatusAsync(cmd);

        dto.Status.Should().Be(TStatus.Completed);
        taskRepo.Verify(r => r.ReloadAsync(task, It.IsAny<CancellationToken>()), Times.Once);
        taskRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Create_CallsGetByIdForUpdateAsync()
    {
        var empRepo = new Mock<IEmployeeRepository>();
        var taskRepo = new Mock<ITaskRepository>();

        var creator = CreateEmployee(isActive: true);
        var assignee = CreateEmployee(isActive: true);

        empRepo.Setup(r => r.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creator);
        empRepo.Setup(r => r.GetByIdForUpdateAsync(assignee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignee);

        taskRepo.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        var svc = CreateService(empRepo, taskRepo);
        var cmd = CreateCommand(creator.Id, assignee.Id);

        var dto = await svc.CreateAsync(cmd);

        empRepo.Verify(r => r.GetByIdForUpdateAsync(assignee.Id, It.IsAny<CancellationToken>()), Times.Once);
        dto.AssigneeEmployeeId.Should().Be(assignee.Id);
    }
}
