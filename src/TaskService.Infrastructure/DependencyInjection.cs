using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskService.Application.Interfaces;
using TaskService.Infrastructure.Data;
using TaskService.Infrastructure.Repositories;

namespace TaskService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<TaskDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(300); // 5 minutes for long-running operations
            });
        });

        services.AddScoped<ITaskRepository, TaskRepository>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<TaskDbContext>>();
        const int maxRetries = 3;
        const int delayMilliseconds = 2000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

                logger.LogInformation("Applying migrations (Attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                await dbContext.Database.EnsureCreatedAsync();
                logger.LogInformation("Database created successfully");

                // Only seed data if this is not a test environment
                await SeedDummyDataAsync(dbContext);
                logger.LogInformation("Database seeded successfully");

                return; // Success - exit the retry loop
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("database providers"))
            {
                // Silently handle if multiple providers are registered (test scenario)
                // The test factory will handle database setup separately
                logger.LogWarning(ex, "Multiple database providers registered - skipping migrations for test scenario");
                return;
            }
            catch (SqlException ex) when (IsTransientError(ex))
            {
                logger.LogWarning(ex, "Transient database error occurred. Retrying... (Attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    // Exponential backoff: delay increases with each retry
                    int delay = delayMilliseconds * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
                else
                {
                    logger.LogError(ex, "Failed to apply migrations after {MaxRetries} attempts", maxRetries);
                    throw;
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Database operation timed out. Retrying... (Attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    int delay = delayMilliseconds * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
                else
                {
                    logger.LogError(ex, "Failed to apply migrations after {MaxRetries} attempts due to timeout", maxRetries);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error applying migrations");
                throw;
            }
        }
    }

    private static bool IsTransientError(SqlException ex)
    {
        // SQL Server transient error numbers
        // -2: Timeout
        // -1: Network error
        // 2: Connection broken
        // 53: Connection broken
        // 64: Communication link failure
        // 233: Connection initialization error
        // 40197: Transient error (Azure)
        // 40501: Service temporarily busy (Azure)
        // 40613: Database unavailable (Azure)
        int[] transientErrorNumbers = { -2, -1, 2, 53, 64, 233, 40197, 40501, 40613 };

        foreach (SqlError error in ex.Errors)
        {
            if (transientErrorNumbers.Contains(error.Number))
            {
                return true;
            }
        }

        return false;
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