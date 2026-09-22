using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Infrastructure.DependencyInjection;
using Xunit;
using TaskManagement.Infrastructure;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Policies;

namespace TaskManagement.Application.Tests;

public class InfrastructureModelAndRepositoryTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var ctx = new AppDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        return ctx;
    }

    [Fact]
    public void Concurrency_Xmin_Enforced_On_Status_Change()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("concurrency_test_db")
            .Options;

        using (var init = new AppDbContext(options))
        {
            init.Database.EnsureDeleted();
            init.Database.EnsureCreated();

            var e1 = Employee.Create(Guid.NewGuid(), "A", "A", "a@x.com", true, DateTime.UtcNow);
            var e2 = Employee.Create(Guid.NewGuid(), "B", "B", "b@x.com", true, DateTime.UtcNow);
            init.Employees.AddRange(e1, e2);

            var now = DateTime.UtcNow;
            var task = TaskItem.Create(Guid.NewGuid(), "T", null, e1.Id, e2.Id, now, now.AddDays(1));
            init.Tasks.Add(task);

            init.Entry(task).Property("xmin").CurrentValue = (uint)1;

            init.SaveChanges();
        }

        using var ctxA = new AppDbContext(options);
        using var ctxB = new AppDbContext(options);

        var taskA = ctxA.Tasks.First();
        var taskB = ctxB.Tasks.First();

        taskA.ChangeStatus(TaskStatus.Completed, DateTime.UtcNow, new StatusTransitionPolicy());
        ctxA.Entry(taskA).Property("xmin").CurrentValue = (uint)2;
        ctxA.SaveChanges();

        taskB.ChangeStatus(TaskStatus.Cancelled, null, new StatusTransitionPolicy());

        var act = () => ctxB.SaveChanges();

        act.Should().Throw<DbUpdateConcurrencyException>();
    }

    [Fact]
    public void Model_ContainsEntitiesAndConfigurations()
    {
        using var ctx = CreateContext("model_test");

        var model = ctx.Model;
        var employeeType = model.FindEntityType(typeof(Employee));
        var taskType = model.FindEntityType(typeof(TaskItem));

        employeeType.Should().NotBeNull();
        taskType.Should().NotBeNull();

        var statusProp = taskType.FindProperty(nameof(TaskItem.Status));
        statusProp.Should().NotBeNull();
        statusProp.ClrType.FullName.Should().Contain("TaskManagement.Domain.Enums.TaskStatus");

        var idxNames = taskType.GetIndexes().Select(i => string.Join("_", i.Properties.Select(p => p.Name))).ToList();
        idxNames.Should().Contain(s => s.Contains(nameof(TaskItem.AssigneeEmployeeId)));
        idxNames.Should().Contain(s => s.Contains(nameof(TaskItem.Status)));
        idxNames.Should().Contain(s => s.Contains(nameof(TaskItem.DueAt)));

        var fks = taskType.GetForeignKeys();
        fks.Should().NotBeEmpty();
    }

    [Fact]
    public void TaskRepository_GetByAssignee_ReturnsOrdered()
    {
        var services = new ServiceCollection();
        services.AddTaskManagementInfrastructure(b => b.UseInMemoryDatabase("repo_test_db"));
        var provider = services.BuildServiceProvider();

        using var ctx = provider.GetRequiredService<AppDbContext>();

        var e1 = Employee.Create(Guid.NewGuid(), "A", "A", "a@x.com", true, DateTime.UtcNow);
        var e2 = Employee.Create(Guid.NewGuid(), "B", "B", "b@x.com", true, DateTime.UtcNow);
        ctx.Employees.AddRange(e1, e2);

        var now = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime();

        var t1 = TaskItem.Create(Guid.NewGuid(), "T1", null, e1.Id, e2.Id, now, now.AddDays(1));
        var t2 = TaskItem.Create(Guid.NewGuid(), "T2", null, e1.Id, e2.Id, now, now.AddDays(1));
        var t3 = TaskItem.Create(Guid.NewGuid(), "T3", null, e1.Id, e2.Id, now, now.AddDays(2));

        typeof(TaskItem).GetProperty("CreatedAt")!.SetValue(t1, now);
        typeof(TaskItem).GetProperty("CreatedAt")!.SetValue(t2, now.AddMinutes(1));

        ctx.Tasks.AddRange(t1, t2, t3);
        ctx.SaveChanges();

        var repo = provider.GetRequiredService<Interfaces.ITaskRepository>();
        var list = repo.GetByAssigneeAsync(e2.Id).GetAwaiter().GetResult();

        list.Should().HaveCount(3);
        list[0].Title.Should().Be("T1");
        list[1].Title.Should().Be("T2");
        list[2].Title.Should().Be("T3");
    }
}
