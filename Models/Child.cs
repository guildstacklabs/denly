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

    [Column("first_name")]
    [JsonProperty("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("middle_name")]
    [JsonProperty("middle_name")]
    public string? MiddleName { get; set; }

    [Column("last_name")]
    [JsonProperty("last_name")]
    public string? LastName { get; set; }

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

    [Column("deactivated_at")]
    [JsonProperty("deactivated_at")]
    public DateTime? DeactivatedAt { get; set; }

    // Helper property for age calculation
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int? Age => BirthDate.HasValue
        ? (int)((DateTime.Today - BirthDate.Value).TotalDays / 365.25)
        : null;

    // Helper property to check if child is active
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsActive => DeactivatedAt == null;

    // Helper property for full name (used for validation)
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string FullName
    {
        get
        {
            var parts = new List<string> { FirstName };
            if (!string.IsNullOrWhiteSpace(MiddleName)) parts.Add(MiddleName);
            if (!string.IsNullOrWhiteSpace(LastName)) parts.Add(LastName);
            return string.Join(" ", parts);
        }
    }
}
