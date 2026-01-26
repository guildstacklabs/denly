using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("event_seen")]
public class EventSeenState : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("event_id")]
    [JsonProperty("event_id")]
    public string EventId { get; set; } = string.Empty;

    [Column("user_id")]
    [JsonProperty("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("last_seen_at")]
    [JsonProperty("last_seen_at")]
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
