using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [Column("email")]
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [Column("name")]
    [JsonProperty("name")]
    public string? Name { get; set; }

    [Column("avatar_url")]
    [JsonProperty("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Helper property for display
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayName => Name ?? Email;
}
