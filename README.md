# Task Management

Small .NET 8 backend demonstrating Domain entities, EF Core (Postgres) mappings, optimistic concurrency using PostgreSQL xmin (shadow property), and a Console demo that seeds and prints sample tasks.

Quick start (requirements: .NET 8 SDK, Docker)

1. Start PostgreSQL:
   docker compose up -d

2. Restore local dotnet tools and apply EF migrations (ConsoleDemo is the startup project):
   dotnet tool restore
   ## PowerShell example:
   $env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=task_management;Username=task_user;Password=task_password"
   dotnet tool run dotnet-ef database update -p src/TaskManagement.Infrastructure -s src/TaskManagement.ConsoleDemo

3. Run the console demo (runs the seeder and prints tasks):
   dotnet run --project src/TaskManagement.ConsoleDemo

4. Run tests:
   dotnet test

Where to look
- Domain: src/TaskManagement.Domain
- Application services: src/TaskManagement.Application
- EF Core + migrations: src/TaskManagement.Infrastructure
- Console demo: src/TaskManagement.ConsoleDemo
- Tests: tests
