using RememberTheMilk;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

var config = AppConfig.Load();
var repository = new TaskRepository();
var notifier = new NotificationService(config.NtfyUrl);
var parser = new TaskParser(config.Model);

using var cts = new CancellationTokenSource();
var schedulerRun = new Scheduler(repository, notifier).RunAsync(cts.Token);

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();

app.MapPost("/tasks", async (TaskRequest body) =>
{
    if (string.IsNullOrWhiteSpace(body.Query))
        return Results.BadRequest(new { error = "Query is required." });

    try
    {
        var task = await parser.ParseQueryAsync(body.Query);
        if (task is null)
            return Results.UnprocessableEntity(new { error = "Could not understand that request." });
        repository.Add(task);
        Log.Info($"Added: \"{task.Description}\" ({task.ScheduleType}), next: {task.NextOccurrence:yyyy-MM-dd HH:mm}");
        return Results.Created($"/tasks/{task.Id}", task);
    }
    catch (Exception ex)
    {
        Log.Info($"Could not add task: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/tasks", () => Results.Ok(repository.GetAll()));

Log.Info($"API listening on http://localhost:{config.Port}");
app.Run($"http://localhost:{config.Port}");
cts.Cancel();

public record TaskRequest(string Query);