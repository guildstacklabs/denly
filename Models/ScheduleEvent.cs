namespace Denly.Models;

public enum EventType
{
    Handoff,
    Health,
    School,
    Activity,
    Family,
    Other
}

public class ScheduleEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public TimeSpan? Time { get; set; }
    public EventType Type { get; set; } = EventType.Other;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class EventTypeExtensions
{
    public static string GetDisplayName(this EventType type) => type switch
    {
        EventType.Handoff => "Handoff",
        EventType.Health => "Doctor/Health",
        EventType.School => "School",
        EventType.Activity => "Activity",
        EventType.Family => "Family",
        EventType.Other => "Other",
        _ => "Other"
    };

    public static string GetColor(this EventType type) => type switch
    {
        EventType.Handoff => "#e07a5f",      // Warm terracotta - transitions between homes
        EventType.Health => "#81b29a",       // Sage green - health/wellness
        EventType.School => "#f2cc8f",       // Soft gold - school events
        EventType.Activity => "#3d85c6",     // Calm blue - activities
        EventType.Family => "#a78bba",       // Soft purple - family gatherings
        EventType.Other => "#9ca3af",        // Neutral gray
        _ => "#9ca3af"
    };
}
