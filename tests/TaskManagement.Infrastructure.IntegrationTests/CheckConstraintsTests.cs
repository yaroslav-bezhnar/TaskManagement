using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.Infrastructure.IntegrationTests;

public class CheckConstraintsTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task DueBeforePlannedStart_ViolatesCheckConstraint_AtDbLevel()
    {
        await using var db = fixture.CreateContext();

        var e1 = Employee.Create(Guid.NewGuid(), "A", "A", $"{Guid.NewGuid():N}@x.com");
        var e2 = Employee.Create(Guid.NewGuid(), "B", "B", $"{Guid.NewGuid():N}@x.com");
        db.Employees.AddRange(e1, e2);
        await db.SaveChangesAsync();

        var due = DateTime.UtcNow;
        var start = due.AddDays(1);

        var act = async () => await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO tasks (id, title, created_by_employee_id, assignee_employee_id, planned_start_at, due_at, status, created_at, updated_at) VALUES ({Guid.NewGuid()}, 'x', {e1.Id}, {e2.Id}, {start}, {due}, 'Pending', {DateTime.UtcNow}, {DateTime.UtcNow})");

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("23514");
        ex.Which.ConstraintName.Should().Be("ck_tasks_due_after_start");
    }

    [Fact]
    public async Task CompletedWithoutCompletedAt_ViolatesCheckConstraint_AtDbLevel()
    {
        await using var db = fixture.CreateContext();

        var e1 = Employee.Create(Guid.NewGuid(), "A", "A", $"{Guid.NewGuid():N}@x.com");
        var e2 = Employee.Create(Guid.NewGuid(), "B", "B", $"{Guid.NewGuid():N}@x.com");
        db.Employees.AddRange(e1, e2);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        var act = async () => await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO tasks (id, title, created_by_employee_id, assignee_employee_id, planned_start_at, due_at, status, completed_at, created_at, updated_at) VALUES ({Guid.NewGuid()}, 'x', {e1.Id}, {e2.Id}, {now}, {now.AddDays(1)}, 'Completed', NULL, {now}, {now})");

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.ConstraintName.Should().Be("ck_tasks_completed_at_required");
    }

    [Fact]
    public async Task SelfAssignedTask_ViolatesCheckConstraint_AtDbLevel()
    {
        await using var db = fixture.CreateContext();

        var e1 = Employee.Create(Guid.NewGuid(), "A", "A", $"{Guid.NewGuid():N}@x.com");
        db.Employees.Add(e1);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        var act = async () => await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO tasks (id, title, created_by_employee_id, assignee_employee_id, planned_start_at, due_at, status, created_at, updated_at) VALUES ({Guid.NewGuid()}, 'x', {e1.Id}, {e1.Id}, {now}, {now.AddDays(1)}, 'Pending', {now}, {now})");

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.ConstraintName.Should().Be("ck_tasks_creator_assignee_different");
    }
}
