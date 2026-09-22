using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Policies;
using TaskManagement.Infrastructure.Repositories;
using Xunit;
using TStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Infrastructure.IntegrationTests;

public class ConcurrencyRetryEndToEndTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TwoUsersChangeStatusSimultaneously_SecondRetriesAndSucceeds()
    {
        var creator = Employee.Create(Guid.NewGuid(), "A", "A", $"{Guid.NewGuid():N}@x.com");
        var assignee = Employee.Create(Guid.NewGuid(), "B", "B", $"{Guid.NewGuid():N}@x.com");
        var taskId = Guid.NewGuid();

        await using (var setup = fixture.CreateContext())
        {
            setup.Employees.AddRange(creator, assignee);
            setup.Tasks.Add(TaskItem.Create(taskId, "T", null, creator.Id, assignee.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
            await setup.SaveChangesAsync();
        }

        await using var ctxUser1 = fixture.CreateContext();
        await using var ctxUser2 = fixture.CreateContext();

        var taskRepo2 = new TaskRepository(ctxUser2);
        var svc2 = new TaskApplicationService(new EmployeeRepository(ctxUser2), taskRepo2, new StatusTransitionPolicy());

        var staleForUser2 = await taskRepo2.GetByIdAsync(taskId);

        var readTask1 = ctxUser1.Tasks.First(t => t.Id == taskId);
        readTask1.ChangeStatus(TStatus.InProgress, null, new StatusTransitionPolicy());
        await ctxUser1.SaveChangesAsync();

        staleForUser2!.ChangeStatus(TStatus.Completed, null, new StatusTransitionPolicy());

        var act = () => taskRepo2.SaveChangesAsync();
        await act.Should().ThrowAsync<ConcurrencyConflictException>();

        await taskRepo2.ReloadAsync(staleForUser2);
        staleForUser2.ChangeStatus(TStatus.Completed, null, new StatusTransitionPolicy());
        await taskRepo2.SaveChangesAsync(); // тепер успішно

        staleForUser2.Status.Should().Be(TStatus.Completed);
    }

    [Fact]
    public async Task ExecuteInTransaction_RollsBackAfterSuccessfulInsert_WhenLaterStepThrows()
    {
        await using var ctx = fixture.CreateContext();
        var repo = new TaskRepository(ctx);

        var e1 = Employee.Create(Guid.NewGuid(), "A", "A", $"{Guid.NewGuid():N}@x.com");
        var e2 = Employee.Create(Guid.NewGuid(), "B", "B", $"{Guid.NewGuid():N}@x.com");
        ctx.Employees.AddRange(e1, e2);
        await ctx.SaveChangesAsync();

        var task = TaskItem.Create(Guid.NewGuid(), "T", null, e1.Id, e2.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        var act = () => repo.ExecuteInTransactionAsync(async () =>
        {
            await repo.AddAsync(task);
            await ctx.SaveChangesAsync();
            throw new InvalidOperationException("boom");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var verifyCtx = fixture.CreateContext();
        (await verifyCtx.Tasks.AnyAsync(t => t.Id == task.Id)).Should().BeFalse();
    }
}
