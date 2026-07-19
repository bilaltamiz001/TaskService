using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskService.Domain.Enums;

namespace TaskService.Application.Models;

public class UpdateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public TaskItemStatus Status { get; set; }

    [Range(0, double.MaxValue)]
    [JsonPropertyName("originalEstimatedWork")]
    public decimal OriginalEstimatedWork { get; set; }
}
