using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("invite_attempts")]
public class InviteAttempt : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    [JsonProperty("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("attempted_at")]
    [JsonProperty("attempted_at")]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    [Column("success")]
    [JsonProperty("success")]
    public bool Success { get; set; }
}
