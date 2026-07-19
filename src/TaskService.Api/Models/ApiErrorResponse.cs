namespace TaskService.Api.Models;

public class ApiErrorResponse
{
    public int StatusCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public IDictionary<string, string[]>? Errors { get; set; }

    public string? TraceId { get; set; }
}
