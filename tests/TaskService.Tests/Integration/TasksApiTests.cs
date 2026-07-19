using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskService.Application.Models;
using TaskService.Domain.Enums;
using TaskService.Infrastructure.Data;

namespace TaskService.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration to use in-memory SQLite for tests
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Set test environment
            context.HostingEnvironment.EnvironmentName = "Test";

            // Override with in-memory SQLite configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DatabaseProvider", "Sqlite" },
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove any existing DbContext registrations to prevent provider conflicts
            services.RemoveAll(typeof(DbContextOptions<TaskDbContext>));
            services.RemoveAll(typeof(TaskDbContext));

            // Create in-memory SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Register DbContext with in-memory SQLite
            services.AddDbContext<TaskDbContext>(
                options => options.UseSqlite(_connection),
                contextLifetime: ServiceLifetime.Scoped);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}

public class TasksApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public TasksApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Task_Lifecycle_Works()
    {
        var createRequest = new CreateTaskRequest
        {
            Title = "Implement API",
            Description = "Build task service endpoints",
            Status = TaskItemStatus.Todo,
            OriginalEstimatedWork = 8
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(createRequest.Title, created.Title);
        Assert.Equal(createRequest.Description, created.Description);
        Assert.Equal(createRequest.Status, created.Status);
        Assert.Equal(createRequest.OriginalEstimatedWork, created.OriginalEstimatedWork);

        var getResponse = await _client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateRequest = new UpdateTaskRequest
        {
            Title = "Implement API - updated",
            Description = "Updated description",
            Status = TaskItemStatus.InProgress,
            OriginalEstimatedWork = 10
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(updateRequest.Title, updated.Title);
        Assert.Equal(TaskItemStatus.InProgress, updated.Status);

        var listResponse = await _client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var tasks = await listResponse.Content.ReadFromJsonAsync<List<TaskResponse>>(JsonOptions);
        Assert.NotNull(tasks);
        Assert.Contains(tasks, task => task.Id == created.Id);

        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await _client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidPayload_ReturnsBadRequest()
    {
        var invalidRequest = new
        {
            title = "",
            originalEstimatedWork = -1
        };

        var response = await _client.PostAsJsonAsync("/api/tasks", invalidRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}