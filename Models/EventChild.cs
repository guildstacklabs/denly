using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("event_children")]
public class EventChild : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("event_id")]
    [JsonProperty("event_id")]
    public string EventId { get; set; } = string.Empty;

    [Column("child_id")]
    [JsonProperty("child_id")]
    public string ChildId { get; set; } = string.Empty;

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
