using System.ComponentModel.DataAnnotations;
using TaskService.Domain.Enums;

namespace TaskService.Application.Models;

public class CreateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    [Range(0, double.MaxValue)]
    public decimal OriginalEstimatedWork { get; set; }
}
