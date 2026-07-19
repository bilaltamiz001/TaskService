using TaskService.Domain.Entities;

namespace TaskService.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task<TaskItem?> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
