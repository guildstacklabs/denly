using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("event_attachments")]
public class EventAttachment : BaseModel
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

    [Column("title")]
    [JsonProperty("title")]
    public string? Title { get; set; }

    [Column("file_url")]
    [JsonProperty("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [Column("created_by")]
    [JsonProperty("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
