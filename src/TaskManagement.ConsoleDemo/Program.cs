using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.DependencyInjection;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, cfg) => cfg.AddEnvironmentVariables())
    .ConfigureServices((context, services) =>
    {
        var conn = context.Configuration["ConnectionStrings__Postgres"] ?? "Host=localhost;Database=task_management;Username=task_user;Password=task_password";
        services.AddTaskManagementInfrastructure(b => b.UseNpgsql(conn));
        services.AddScoped<TaskManagement.Application.Services.TaskApplicationService>();
        services.AddScoped<DatabaseSeeder>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
Console.WriteLine("Running seeder...");
await seeder.SeedAsync();
Console.WriteLine("Done.");

// Demo: print tasks for assignee
var svc = scope.ServiceProvider.GetRequiredService<TaskManagement.Application.Services.TaskApplicationService>();
var tasks = await svc.GetByAssigneeAsync(Guid.Parse("22222222-2222-2222-2222-222222222222"));
foreach (var t in tasks)
    Console.WriteLine($"Task: {t.Title} - {t.Status} - Due {t.DueAt:u}");

return 0;
