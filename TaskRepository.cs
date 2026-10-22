namespace RememberTheMilk;

public class TaskRepository
{
    private readonly List<ScheduledTask> _tasks = new();
    private readonly object _lock = new();

    public void Add(ScheduledTask task)
    {
        lock (_lock) { _tasks.Add(task); }
    }

    public List<ScheduledTask> GetAll()
    {
        lock (_lock) { return _tasks.ToList(); }
    }

    public void Remove(ScheduledTask task)
    {
        lock (_lock) { _tasks.RemoveAll(t => t.Id == task.Id); }
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
        }
    }
}
