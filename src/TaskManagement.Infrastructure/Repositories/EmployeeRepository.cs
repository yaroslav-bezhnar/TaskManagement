using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Repositories;

internal class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;

    public EmployeeRepository(AppDbContext db) => _db = db;

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Employees.FromSqlInterpolated($"SELECT * FROM \"employees\" WHERE \"id\" = {id} FOR UPDATE").FirstOrDefaultAsync(cancellationToken);
}
