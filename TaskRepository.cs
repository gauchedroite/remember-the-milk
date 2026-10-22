using System.Text.Json;
using System.Text.Json.Serialization;

namespace RememberTheMilk;

public class TaskRepository
{
    private const string FileName = "tasks.json";
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly List<ScheduledTask> _tasks = new();
    private readonly object _lock = new();

    public TaskRepository()
    {
        if (File.Exists(FileName))
        {
            var loaded = JsonSerializer.Deserialize<List<ScheduledTask>>(File.ReadAllText(FileName), JsonOpts);
            if (loaded is not null)
                _tasks = loaded;
        }
    }

    // Write to a temp file first: a crash mid-write must not destroy the task list.
    private void Persist()
    {
        var tmp = FileName + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_tasks, JsonOpts));
        File.Move(tmp, FileName, overwrite: true);
    }

    public void Add(ScheduledTask task)
    {
        lock (_lock) { _tasks.Add(task); Persist(); }
    }

    public List<ScheduledTask> GetAll()
    {
        lock (_lock) { return _tasks.ToList(); }
    }

    public bool RemoveById(Guid id)
    {
        lock (_lock)
        {
            if (_tasks.RemoveAll(t => t.Id == id) == 0) return false;
            Persist();
            return true;
        }
    }

    public List<ScheduledTask> GetDue(DateTime now)
    {
        lock (_lock)
        {
            return _tasks.Where(t => t.NextOccurrence.HasValue && t.NextOccurrence.Value <= now).ToList();
        }
    }

    // Bump past-due recurring tasks to their next future occurrence.
    public void Advance(ScheduledTask task)
    {
        var increment = task.ScheduleType switch
        {
            ScheduleType.Daily => TimeSpan.FromDays(1),
            ScheduleType.Weekly => TimeSpan.FromDays(7),
            ScheduleType.Monthly => TimeSpan.FromDays(30),
            _ => TimeSpan.Zero
        };

        lock (_lock)
        {
            var stored = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (stored?.NextOccurrence is null) return;
            var next = stored.NextOccurrence.Value + increment;
            while (next <= DateTime.Now) next += increment;
            stored.NextOccurrence = next;
            Persist();
        }
    }
}