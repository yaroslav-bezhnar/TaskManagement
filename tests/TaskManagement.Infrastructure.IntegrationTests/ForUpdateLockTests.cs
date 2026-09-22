using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Repositories;
using Xunit;

namespace TaskManagement.Infrastructure.IntegrationTests;

public class ForUpdateLockTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task GetByIdForUpdate_BlocksConcurrentUpdate_UntilTransactionCommits()
    {
        var employeeId = Guid.NewGuid();

        await using (var setup = fixture.CreateContext())
        {
            setup.Employees.Add(Employee.Create(employeeId, "A", "A", $"{Guid.NewGuid():N}@x.com"));
            await setup.SaveChangesAsync();
        }

        await using var ctx1 = fixture.CreateContext();
        await using var tx1 = await ctx1.Database.BeginTransactionAsync();
        var repo1 = new EmployeeRepository(ctx1);

        await repo1.GetByIdForUpdateAsync(employeeId);

        var sw = Stopwatch.StartNew();
        var blockedUpdate = Task.Run(async () =>
        {
            await using var ctx2 = fixture.CreateContext();
            var emp = await ctx2.Employees.FirstAsync(e => e.Id == employeeId);

            await ctx2.Database.ExecuteSqlInterpolatedAsync($"UPDATE employees SET is_active = false WHERE id = {employeeId}");
        });

        await Task.Delay(500);
        await tx1.CommitAsync();
        await blockedUpdate;

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(450);
    }
}
