using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

public enum ExpenseCategory
{
    Medical,
    School,
    Activities,
    Clothing,
    Childcare,
    Other
}

[Table("expenses")]
public class Expense : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("child_id")]
    [JsonProperty("child_id")]
    public string? ChildId { get; set; }

    [Column("description")]
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [Column("amount")]
    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [Column("paid_by")]
    [JsonProperty("paid_by")]
    public string PaidBy { get; set; } = string.Empty; // UUID referencing profiles

    [Column("receipt_url")]
    [JsonProperty("receipt_url")]
    public string? ReceiptUrl { get; set; }

    [Column("created_by")]
    [JsonProperty("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("settled_at")]
    [JsonProperty("settled_at")]
    public DateTime? SettledAt { get; set; }

    // Helper properties for UI
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSettled => SettledAt.HasValue;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? PaidByName { get; set; } // Populated from profiles
}

[Table("settlements")]
public class Settlement : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("den_id")]
    [JsonProperty("den_id")]
    public string DenId { get; set; } = string.Empty;

    [Column("from_user_id")]
    [JsonProperty("from_user_id")]
    public string FromUserId { get; set; } = string.Empty;

    [Column("to_user_id")]
    [JsonProperty("to_user_id")]
    public string ToUserId { get; set; } = string.Empty;

    [Column("amount")]
    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [Column("note")]
    [JsonProperty("note")]
    public string? Note { get; set; }

    [Column("created_by")]
    [JsonProperty("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Helper properties for UI
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? FromUserName { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ToUserName { get; set; }
}

public static class ExpenseCategoryExtensions
{
    public static string GetDisplayName(this ExpenseCategory category) => category switch
    {
        ExpenseCategory.Medical => "Medical",
        ExpenseCategory.School => "School",
        ExpenseCategory.Activities => "Activities",
        ExpenseCategory.Clothing => "Clothing",
        ExpenseCategory.Childcare => "Childcare",
        ExpenseCategory.Other => "Other",
        _ => "Other"
    };

    public static string GetColor(this ExpenseCategory category) => category switch
    {
        ExpenseCategory.Medical => "#81b29a",    // Sage green
        ExpenseCategory.School => "#f2cc8f",     // Soft gold
        ExpenseCategory.Activities => "#3d85c6", // Calm blue
        ExpenseCategory.Clothing => "#a78bba",   // Soft purple
        ExpenseCategory.Childcare => "#e07a5f",  // Warm terracotta
        ExpenseCategory.Other => "#9ca3af",      // Neutral gray
        _ => "#9ca3af"
    };
}
