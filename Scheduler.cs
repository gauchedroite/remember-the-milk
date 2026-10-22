namespace RememberTheMilk;

public class Scheduler
{
    private readonly TaskRepository _repository;
    private readonly NotificationService _notifier;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public Scheduler(TaskRepository repository, NotificationService notifier)
    {
        _repository = repository;
        _notifier = notifier;
    }

    public async Task RunAsync(CancellationToken token)
    {
        Log.Info($"[scheduler] running, checking every {_interval.TotalSeconds}s");
        while (!token.IsCancellationRequested)
        {
            try
            {
                foreach (var task in _repository.GetDue(DateTime.Now))
                {
                    try
                    {
                        await _notifier.SendAsync(task);
                        Log.Info($"[scheduler] notified: {task.Description}");

                        if (task.ScheduleType == ScheduleType.Single)
                            _repository.RemoveById(task.Id);
                        else
                            _repository.Advance(task);
                    }
                    catch (Exception ex)
                    {
                        // Task stays due; retried on the next tick.
                        Log.Info($"[scheduler] notify failed for \"{task.Description}\": {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[scheduler] error: {ex.Message}");
            }

            await Task.Delay(_interval, token);
        }
    }
}
