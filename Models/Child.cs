using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("children")]
public class Child : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [Column("birth_date")]
    [JsonProperty("birth_date")]
    public DateTime? BirthDate { get; set; }

    [Column("color")]
    [JsonProperty("color")]
    public string? Color { get; set; }

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Helper property for age calculation
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int? Age => BirthDate.HasValue
        ? (int)((DateTime.Today - BirthDate.Value).TotalDays / 365.25)
        : null;
}
