using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

[Table("expense_splits")]
public class ExpenseSplit : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("expense_id")]
    [JsonProperty("expense_id")]
    public string ExpenseId { get; set; } = string.Empty;

    [Column("user_id")]
    [JsonProperty("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("percent")]
    [JsonProperty("percent")]
    public decimal Percent { get; set; }

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
