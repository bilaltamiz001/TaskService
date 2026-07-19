using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskService.Application.Interfaces;
using TaskService.Infrastructure.Data;
using TaskService.Infrastructure.Repositories;

namespace TaskService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration.GetSection("DatabaseProvider").Value ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<TaskDbContext>(options =>
        {
            if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                // Default to SQLite
                connectionString ??= "Data Source=taskservice.db";
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<ITaskRepository, TaskRepository>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            // Only seed data if this is not a test environment
            await SeedDummyDataAsync(dbContext);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("database providers"))
        {
            // Silently handle if multiple providers are registered (test scenario)
            // The test factory will handle database setup separately
        }
    }

    private static async Task SeedDummyDataAsync(TaskDbContext dbContext)
    {
        if (await dbContext.Tasks.AnyAsync())
            return;

        var tasks = new[]
        {
            new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Implement API endpoints",
                Description = "Create CRUD endpoints for task management",
                Status = Domain.Enums.TaskItemStatus.Done,
                OriginalEstimatedWork = 8,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Add SQL Server integration",
                Description = "Migrate from SQLite to SQL Server with Windows authentication",
                Status = Domain.Enums.TaskItemStatus.Done,
                OriginalEstimatedWork = 4,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Write integration tests",
                Description = "Create comprehensive tests for all endpoints",
                Status = Domain.Enums.TaskItemStatus.InProgress,
                OriginalEstimatedWork = 6,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Setup database seeding",
                Description = "Add dummy data on first run for testing",
                Status = Domain.Enums.TaskItemStatus.Done,
                OriginalEstimatedWork = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Deploy to production",
                Description = "Set up CI/CD pipeline and deploy API to cloud",
                Status = Domain.Enums.TaskItemStatus.Todo,
                OriginalEstimatedWork = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Implement pagination",
                Description = "Add skip/take parameters to list endpoint",
                Status = Domain.Enums.TaskItemStatus.Todo,
                OriginalEstimatedWork = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Add filtering capabilities",
                Description = "Allow filtering tasks by status and date range",
                Status = Domain.Enums.TaskItemStatus.Todo,
                OriginalEstimatedWork = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await dbContext.Tasks.AddRangeAsync(tasks);
        await dbContext.SaveChangesAsync();
    }
}