using TaskService.Application.Exceptions;
using TaskService.Application.Interfaces;
using TaskService.Application.Models;
using TaskService.Domain.Entities;

namespace TaskService.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = request.Status,
            OriginalEstimatedWork = request.OriginalEstimatedWork,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        var created = await _taskRepository.AddAsync(task, cancellationToken);
        return MapToResponse(created);
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        return MapToResponse(task);
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        return tasks.Select(MapToResponse).ToList();
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        existing.Title = request.Title.Trim();
        existing.Description = request.Description?.Trim();
        existing.Status = request.Status;
        existing.OriginalEstimatedWork = request.OriginalEstimatedWork;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _taskRepository.UpdateAsync(existing, cancellationToken)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        return MapToResponse(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _taskRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundException($"Task with id '{id}' was not found.");
        }
    }

    private static TaskResponse MapToResponse(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        OriginalEstimatedWork = task.OriginalEstimatedWork,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
