using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Infrastructure;

public class DatabaseSeeder
{
    private readonly AppDbContext _db;

    public DatabaseSeeder(AppDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var e1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var e2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var e3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

        if (!await _db.Employees.AnyAsync(cancellationToken))
        {
            var now = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime();
            var e1 = Employee.Create(e1Id, "Alice", "Smith", "alice@example.com", true, now);
            var e2 = Employee.Create(e2Id, "Bob", "Jones", "bob@example.com", true, now);
            var e3 = Employee.Create(e3Id, "Carol", "Stone", "carol@example.com", false, now);
            _db.Employees.AddRange(e1, e2, e3);
        }

        if (!await _db.Tasks.AnyAsync(cancellationToken))
        {
            var now = DateTime.Parse("2026-01-02T00:00:00Z").ToUniversalTime();
            var t1 = TaskItem.Create(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Task One", string.Empty, Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), now, now.AddDays(1));
            var t2 = TaskItem.Create(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Task Two", null, Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), now.AddDays(2), now.AddDays(3));
            var t3 = TaskItem.Create(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Task Three", "done", Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), now.AddDays(-2), now.AddDays(-1));
            t3.ChangeStatus(TStatus.Completed, now.AddDays(-1), new Domain.Policies.StatusTransitionPolicy());
            _db.Tasks.AddRange(t1, t2, t3);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
