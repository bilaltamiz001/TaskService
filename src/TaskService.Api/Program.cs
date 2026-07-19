using Microsoft.AspNetCore.Mvc;
using TaskService.Api.Middleware;
using TaskService.Api.Models;
using TaskService.Application;
using TaskService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            var response = new ApiErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Validation failed.",
                Errors = errors,
                TraceId = context.HttpContext.TraceIdentifier
            };

            return new BadRequestObjectResult(response);
        };
    });

// Add Swagger/OpenAPI configuration
builder.Services.AddOpenApi();
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled", false);
if (swaggerEnabled)
{
    builder.Services.AddSwaggerGen();
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

await app.Services.ApplyMigrationsAsync();

app.UseExceptionHandling();

if (swaggerEnabled || app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    if (swaggerEnabled)
    {
        // Add Swagger UI
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            var routePrefix = app.Configuration.GetValue<string>("Swagger:RoutePrefix", "swagger") ?? "swagger";
            var docTitle = app.Configuration.GetValue<string>("Swagger:DocumentTitle", "Task Service API Documentation") ?? "Task Service API Documentation";

            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Service API v1");
            options.RoutePrefix = routePrefix;

            // UI Configuration
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelExpandDepth(2);
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.ShowExtensions();
            options.EnableFilter();

            // Custom title
            options.DocumentTitle = docTitle;
        });
    }
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;