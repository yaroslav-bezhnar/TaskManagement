using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Policies;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagementInfrastructure(this IServiceCollection services, Action<DbContextOptionsBuilder>? configureDb = null)
    {
        services.AddSingleton(new StatusTransitionPolicy());
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddDbContext<AppDbContext>(configureDb ?? (b => b.UseNpgsql("Host=localhost;Database=task_management;Username=task_user;Password=task_password")));

        return services;
    }
}
