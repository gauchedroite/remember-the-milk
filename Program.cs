using RememberTheMilk;

var repository = new TaskRepository();
var notifier = new NotificationService();
var parser = new TaskParser();

using var cts = new CancellationTokenSource();
var schedulerRun = new Scheduler(repository, notifier).RunAsync(cts.Token);

Console.WriteLine("Task scheduler ready. Type a reminder request, or 'list' to show scheduled tasks. Ctrl+C to exit.");

while (true)
{
    var query = Console.ReadLine();
    if (query is null or "exit" or "quit") break;

    if (System.Text.RegularExpressions.Regex.IsMatch(query, @"\b(list|show)\b.*\b(tasks?|reminders?)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
    {
        ListTasks(repository.GetAll());
        continue;
    }

    try
    {
        var task = await parser.ParseQueryAsync(query);
        if (task is null)
        {
            Console.WriteLine("Could not understand that request.");
            continue;
        }
        repository.Add(task);
        Console.WriteLine($"Added: \"{task.Description}\" ({task.ScheduleType}), next: {task.NextOccurrence:yyyy-MM-dd HH:mm}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not add task: {ex.Message}");
    }
}

cts.Cancel();

static void ListTasks(List<ScheduledTask> tasks)
{
    if (tasks.Count == 0)
    {
        Console.WriteLine("No scheduled tasks.");
        return;
    }

    Console.WriteLine($"\nScheduled tasks ({tasks.Count}):");
    foreach (var task in tasks)
    {
        Console.WriteLine($"  [{task.Id.ToString()[..8]}] {task.Description}");
        Console.WriteLine($"      {task.ScheduleType}, next: {task.NextOccurrence?.ToString("yyyy-MM-dd HH:mm") ?? "n/a"}");
    }
}
