using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Denly.Models;

public enum DocumentFolder
{
    Medical,
    School,
    Legal,
    Identity,
    Other
}

[Table("documents")]
public class Document : BaseModel
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

    [Column("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [Column("category")]
    [JsonProperty("category")]
    public string CategoryString
    {
        get => Folder.ToString().ToLowerInvariant();
        set => Folder = Enum.TryParse<DocumentFolder>(value, true, out var result) ? result : DocumentFolder.Other;
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public DocumentFolder Folder { get; set; } = DocumentFolder.Other;

    [Column("file_url")]
    [JsonProperty("file_url")]
    public string? FileUrl { get; set; }

    [Column("uploaded_by")]
    [JsonProperty("uploaded_by")]
    public string UploadedBy { get; set; } = string.Empty;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Helper property for UI compatibility (maps Title to Name)
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Name
    {
        get => Title;
        set => Title = value;
    }

    // Helper property for file name extraction
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? FileName => !string.IsNullOrEmpty(FileUrl)
        ? System.IO.Path.GetFileName(new Uri(FileUrl).LocalPath)
        : null;
}

public static class DocumentFolderExtensions
{
    public static string GetDisplayName(this DocumentFolder folder) => folder switch
    {
        DocumentFolder.Medical => "Medical",
        DocumentFolder.School => "School",
        DocumentFolder.Legal => "Legal",
        DocumentFolder.Identity => "Identity",
        DocumentFolder.Other => "Other",
        _ => "Other"
    };

    public static string GetColor(this DocumentFolder folder) => folder switch
    {
        DocumentFolder.Medical => "var(--color-category-medical)",
        DocumentFolder.School => "var(--color-category-school)",
        DocumentFolder.Legal => "var(--color-category-legal)",
        DocumentFolder.Identity => "var(--color-category-identity)",
        DocumentFolder.Other => "var(--color-category-other)",
        _ => "var(--color-category-other)"
    };

    public static string GetIcon(this DocumentFolder folder) => folder switch
    {
        DocumentFolder.Medical => "M12 4v4m0 0v4m0-4h4m-4 0H8m13 4v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5m16-4V7a2 2 0 00-2-2H5a2 2 0 00-2 2v5",
        DocumentFolder.School => "M12 14l9-5-9-5-9 5 9 5zm0 7l-9-5v-2l9 5 9-5v2l-9 5z",
        DocumentFolder.Legal => "M3 6l9-4 9 4v2H3V6zm1 4h16v10a2 2 0 01-2 2H6a2 2 0 01-2-2V10zm5 3v4m4-4v4",
        DocumentFolder.Identity => "M10 6H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V8a2 2 0 00-2-2h-5m-4 0V5a2 2 0 114 0v1m-4 0a2 2 0 104 0",
        DocumentFolder.Other => "M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z",
        _ => "M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"
    };
}
