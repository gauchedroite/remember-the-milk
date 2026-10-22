namespace RememberTheMilk;

public enum ScheduleType
{
    Single,
    Daily,
    Weekly,
    Monthly
}

public class ScheduledTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = "";
    public ScheduleType ScheduleType { get; set; }
    public DateTime? NextOccurrence { get; set; }
    public string? NtfyTitle { get; set; }
    public string RawQuery { get; set; } = "";
}
