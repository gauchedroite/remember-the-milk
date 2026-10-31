using System.Text.Json;
using System.Text.Json.Serialization;

namespace RememberTheMilk;

public class TaskRepository
{
    // Data lives in %LOCALAPPDATA%, not next to the exe: publish/deploy folders get wiped.
    private static readonly string FileName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GaucheDroite", "RememberTheMilk", "tasks.json");
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly List<ScheduledTask> _tasks = new();
    private readonly object _lock = new();

    public TaskRepository()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FileName)!);

        if (File.Exists(FileName))
        {
            var loaded = JsonSerializer.Deserialize<List<ScheduledTask>>(File.ReadAllText(FileName), JsonOpts);
            if (loaded is not null)
                _tasks = loaded;
        }
    }

    // Copy the last good version first: even a bad write or an accidental delete leaves it recoverable.
    // Write to a temp file and move: a crash mid-write must not destroy the task list.
    private void Persist()
    {
        if (File.Exists(FileName))
        {
            File.Copy(FileName, FileName + ".bak", overwrite: true);
        }

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

    public ScheduledTask? GetById(Guid id)
    {
        lock (_lock) { return _tasks.FirstOrDefault(t => t.Id == id); }
    }

    // Replaces the editable fields of the stored task; the id and RawQuery stay as they were.
    public bool Update(ScheduledTask updated)
    {
        lock (_lock)
        {
            var stored = _tasks.FirstOrDefault(t => t.Id == updated.Id);
            if (stored is null) return false;
            stored.Description = updated.Description;
            stored.ScheduleType = updated.ScheduleType;
            stored.Weekdays = updated.Weekdays;
            stored.NextOccurrence = updated.NextOccurrence;
            stored.IsMuted = updated.IsMuted;
            stored.NtfyTitle = updated.NtfyTitle;
            Persist();
            return true;
        }
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
            DateTime next;
            if (stored.ScheduleType == ScheduleType.Weekly && stored.Weekdays is { Count: > 0 })
            {
                // Same time of day, next calendar day that is one of the wanted weekdays.
                next = stored.NextOccurrence.Value.Date;
                do next = next.AddDays(1);
                while (!stored.Weekdays.Contains(next.DayOfWeek));
                next += stored.NextOccurrence.Value.TimeOfDay;
            }
            else
            {
                next = stored.NextOccurrence.Value + increment;
                while (next <= DateTime.Now) next += increment;
            }
            stored.NextOccurrence = next;
            Persist();
        }
    }
}