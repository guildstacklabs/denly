using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("den_invites")]
public class DenInvite : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("code")]
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [Column("role")]
    [JsonProperty("role")]
    public string Role { get; set; } = "co-parent"; // Role to assign when invite is used

    [Column("created_by")]
    [JsonProperty("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    [JsonProperty("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("used_by")]
    [JsonProperty("used_by")]
    public string? UsedBy { get; set; }

    [Column("used_at")]
    [JsonProperty("used_at")]
    public DateTime? UsedAt { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsUsed => UsedAt.HasValue;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsValid => !IsExpired && !IsUsed;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public TimeSpan TimeRemaining => ExpiresAt - DateTime.UtcNow;

    // Populated when validating invite
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? DenName { get; set; }
}
