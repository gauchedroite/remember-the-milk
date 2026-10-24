using RememberTheMilk;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = AppConfig.Load();
var repository = new TaskRepository();
var notifier = new NotificationService(config.NtfyUrl);
var parser = new TaskParser(config.Model);

using var cts = new CancellationTokenSource();
var schedulerRun = new Scheduler(repository, notifier).RunAsync(cts.Token);

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); // no console noise: request logs, app lifetime messages
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
        if (task.NextOccurrence is null)
            return Results.UnprocessableEntity(new { error = $"No time found in: \"{body.Query}\". Include a date or time, e.g. \"tomorrow at 2\". For recurring tasks use e.g. \"every day\"." });
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

// shortcut: index.html is read from the working directory (like tasks.json) and is not in the publish output; copy it manually when deploying
app.MapGet("/", () => Results.Text(File.ReadAllText("index.html"), "text/html"));

app.MapGet("/tasks/{id}", (Guid id) =>
    repository.GetById(id) is { } task ? Results.Ok(task) : Results.NotFound());

app.MapPut("/tasks/{id}", (Guid id, ScheduledTask body) =>
{
    if (string.IsNullOrWhiteSpace(body.Description))
        return Results.BadRequest(new { error = "Description is required." });
    if (body.NextOccurrence is null)
        return Results.BadRequest(new { error = "NextOccurrence is required." });
    body.Id = id;
    return repository.Update(body) ? Results.Ok(body) : Results.NotFound();
});

app.MapDelete("/tasks/{id}", (Guid id) =>
    repository.RemoveById(id) ? Results.NoContent() : Results.NotFound());

Log.Info($"API listening on http://localhost:{config.Port}");
app.Run($"http://localhost:{config.Port}");
cts.Cancel();

public record TaskRequest(string Query);