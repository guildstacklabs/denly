using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

/// <summary>
/// Represents a device token for push notifications.
/// </summary>
[Table("device_tokens")]
public class DeviceToken : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    [JsonProperty("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("platform")]
    [JsonProperty("platform")]
    public string Platform { get; set; } = string.Empty;

    [Column("token")]
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Platform types for device tokens.
/// </summary>
public static class DevicePlatform
{
    public const string iOS = "ios";
    public const string Android = "android";
}
