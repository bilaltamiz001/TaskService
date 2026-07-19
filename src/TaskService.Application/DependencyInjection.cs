using Microsoft.Extensions.DependencyInjection;
using TaskService.Application.Interfaces;
using TaskService.Application.Services;

namespace TaskService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, Services.TaskService>();
        return services;
    }
}
