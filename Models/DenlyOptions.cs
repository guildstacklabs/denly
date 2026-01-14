namespace Denly.Models;

public class DenlyOptions
{
    public const string SectionName = "Denly";

    public string SupabaseUrl { get; init; } = string.Empty;

    public string SupabaseAnonKey { get; init; } = string.Empty;
}
