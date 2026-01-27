using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

public enum EventType
{
    Handoff,
    Doctor,
    School,
    Activity,
    Family,
    Other
}

[Table("events")]
public class Event : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("child_id")]
    [JsonProperty("child_id")]
    public string? ChildId { get; set; }

    [Column("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [Column("event_type")]
    [JsonProperty("event_type")]
    public string EventTypeString
    {
        get => Type.ToString().ToLowerInvariant();
        set => Type = Enum.TryParse<EventType>(value, true, out var result) ? result : EventType.Other;
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public EventType Type { get; set; } = EventType.Other;

    [Column("starts_at")]
    [JsonProperty("starts_at")]
    public DateTime StartsAt { get; set; } = DateTime.Today;

    [Column("ends_at")]
    [JsonProperty("ends_at")]
    public JToken? EndsAtRaw
    {
        get => _endsAtRaw;
        set
        {
            _endsAtRaw = value;
            _endsAt = ParseNullableDateTime(value);
        }
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? EndsAt
    {
        get => _endsAt;
        set
        {
            _endsAt = value;
            _endsAtRaw = value.HasValue ? new JValue(value.Value) : null;
        }
    }

    [Column("all_day")]
    [JsonProperty("all_day")]
    public bool AllDay { get; set; } = false;

    [Column("location")]
    [JsonProperty("location")]
    public string? Location { get; set; }

    [Column("notes")]
    [JsonProperty("notes")]
    public string? Notes { get; set; }

    [Column("created_by")]
    [JsonProperty("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Helper properties for UI compatibility
    // Convert from UTC (stored) to local time for display
    // Note: Supabase/Newtonsoft might return Local time depending on configuration.
    // We check the Kind to ensure we don't double-convert or misinterpret already Local times.
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime Date => StartsAt.Kind == DateTimeKind.Utc
        ? StartsAt.ToLocalTime().Date
        : StartsAt.Date;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public TimeSpan? Time => AllDay ? null : (StartsAt.Kind == DateTimeKind.Utc ? StartsAt.ToLocalTime() : StartsAt).TimeOfDay;

    private static DateTime? ParseNullableDateTime(JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type == JTokenType.Array)
        {
            var array = (JArray)token;
            var last = array.Last ?? array.First;
            return last?.ToObject<DateTime?>();
        }

        return token.ToObject<DateTime?>();
    }

    private JToken? _endsAtRaw;
    private DateTime? _endsAt;
}

public static class EventTypeExtensions
{
    public static string GetDisplayName(this EventType type) => type switch
    {
        EventType.Handoff => "Handoff",
        EventType.Doctor => "Doctor/Health",
        EventType.School => "School",
        EventType.Activity => "Activity",
        EventType.Family => "Family",
        EventType.Other => "Other",
        _ => "Other"
    };

    public static string GetColor(this EventType type) => type switch
    {
        EventType.Handoff => "var(--color-category-handoff)",
        EventType.Doctor => "var(--color-category-medical)",
        EventType.School => "var(--color-category-school)",
        EventType.Activity => "var(--color-category-activity)",
        EventType.Family => "var(--color-category-family)",
        EventType.Other => "var(--color-category-other)",
        _ => "var(--color-category-other)"
    };
}
