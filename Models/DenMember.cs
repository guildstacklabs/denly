using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("den_members")]
public class DenMember : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("user_id")]
    [JsonProperty("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("role")]
    [JsonProperty("role")]
    public string Role { get; set; } = "co-parent"; // "owner", "co-parent", or "observer"

    [Column("invited_by")]
    [JsonProperty("invited_by")]
    public string? InvitedBy { get; set; }

    [Column("joined_at")]
    [JsonProperty("joined_at")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // UI-only properties populated from profiles table join (not in den_members table)
    // These are ignored by Postgrest serialization
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? Email { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? DisplayName { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? AvatarUrl { get; set; }

    // Computed properties (not stored in database)
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsOwner => Role == "owner";

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsObserver => Role == "observer";
}
