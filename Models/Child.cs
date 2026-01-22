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

    [Column("doctor_name")]
    [JsonProperty("doctor_name")]
    public string? DoctorName { get; set; }

    [Column("doctor_contact")]
    [JsonProperty("doctor_contact")]
    public string? DoctorContact { get; set; }

    [Column("allergies")]
    [JsonProperty("allergies")]
    public string? Allergies { get; set; }

    [Column("school_name")]
    [JsonProperty("school_name")]
    public string? SchoolName { get; set; }

    [Column("clothing_size")]
    [JsonProperty("clothing_size")]
    public string? ClothingSize { get; set; }

    [Column("shoe_size")]
    [JsonProperty("shoe_size")]
    public string? ShoeSize { get; set; }

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
