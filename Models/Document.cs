namespace Denly.Models;

public enum DocumentFolder
{
    Medical,
    School,
    Legal,
    Insurance,
    Other
}

public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DocumentFolder Folder { get; set; } = DocumentFolder.Other;
    public string Notes { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class DocumentFolderExtensions
{
    public static string GetDisplayName(this DocumentFolder folder) => folder switch
    {
        DocumentFolder.Medical => "Medical",
        DocumentFolder.School => "School",
        DocumentFolder.Legal => "Legal",
        DocumentFolder.Insurance => "Insurance",
        DocumentFolder.Other => "Other",
        _ => "Other"
    };

    public static string GetColor(this DocumentFolder folder) => folder switch
    {
        DocumentFolder.Medical => "#81b29a",    // Sage green
        DocumentFolder.School => "#f2cc8f",     // Soft gold
        DocumentFolder.Legal => "#3d85c6",      // Calm blue
        DocumentFolder.Insurance => "#a78bba",  // Soft purple
        DocumentFolder.Other => "#9ca3af",      // Neutral gray
        _ => "#9ca3af"
    };

    public static string GetIcon(this DocumentFolder folder) => folder switch
    {
        DocumentFolder.Medical => "M12 4v4m0 0v4m0-4h4m-4 0H8m13 4v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5m16-4V7a2 2 0 00-2-2H5a2 2 0 00-2 2v5",
        DocumentFolder.School => "M12 14l9-5-9-5-9 5 9 5zm0 7l-9-5v-2l9 5 9-5v2l-9 5z",
        DocumentFolder.Legal => "M3 6l9-4 9 4v2H3V6zm1 4h16v10a2 2 0 01-2 2H6a2 2 0 01-2-2V10zm5 3v4m4-4v4",
        DocumentFolder.Insurance => "M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z",
        DocumentFolder.Other => "M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z",
        _ => "M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"
    };
}
