using System.Text.Json;
using Denly.Models;

namespace Denly.Services;

public interface IDocumentService
{
    Task<List<Document>> GetAllDocumentsAsync();
    Task<List<Document>> GetDocumentsByFolderAsync(DocumentFolder folder);
    Task<List<Document>> SearchDocumentsAsync(string searchTerm);
    Task<List<Document>> GetRecentDocumentsAsync(int count = 3);
    Task<Document?> GetDocumentByIdAsync(string id);
    Task SaveDocumentAsync(Document document);
    Task DeleteDocumentAsync(string id);
    Task<Dictionary<DocumentFolder, int>> GetFolderCountsAsync();
}

public class LocalDocumentService : IDocumentService
{
    private readonly string _filePath;
    private List<Document>? _cache;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LocalDocumentService()
    {
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "documents.json");
    }

    private async Task<List<Document>> LoadDocumentsAsync()
    {
        if (_cache != null)
            return _cache;

        if (!File.Exists(_filePath))
        {
            _cache = new List<Document>();
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _cache = JsonSerializer.Deserialize<List<Document>>(json, _jsonOptions) ?? new List<Document>();
        }
        catch
        {
            _cache = new List<Document>();
        }

        return _cache;
    }

    private async Task SaveDocumentsAsync(List<Document> documents)
    {
        var json = JsonSerializer.Serialize(documents, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
        _cache = documents;
    }

    public async Task<List<Document>> GetAllDocumentsAsync()
    {
        var documents = await LoadDocumentsAsync();
        return documents.OrderBy(d => d.Folder).ThenBy(d => d.Name).ToList();
    }

    public async Task<List<Document>> GetDocumentsByFolderAsync(DocumentFolder folder)
    {
        var documents = await LoadDocumentsAsync();
        return documents
            .Where(d => d.Folder == folder)
            .OrderBy(d => d.Name)
            .ToList();
    }

    public async Task<List<Document>> SearchDocumentsAsync(string searchTerm)
    {
        var documents = await LoadDocumentsAsync();

        if (string.IsNullOrWhiteSpace(searchTerm))
            return documents.OrderBy(d => d.Folder).ThenBy(d => d.Name).ToList();

        var term = searchTerm.ToLowerInvariant();
        return documents
            .Where(d => d.Name.ToLowerInvariant().Contains(term) ||
                        d.Notes.ToLowerInvariant().Contains(term) ||
                        (d.FileName?.ToLowerInvariant().Contains(term) ?? false))
            .OrderBy(d => d.Folder)
            .ThenBy(d => d.Name)
            .ToList();
    }

    public async Task<List<Document>> GetRecentDocumentsAsync(int count = 3)
    {
        var documents = await LoadDocumentsAsync();
        return documents
            .OrderByDescending(d => d.CreatedAt)
            .Take(count)
            .ToList();
    }

    public async Task<Document?> GetDocumentByIdAsync(string id)
    {
        var documents = await LoadDocumentsAsync();
        return documents.FirstOrDefault(d => d.Id == id);
    }

    public async Task SaveDocumentAsync(Document document)
    {
        var documents = await LoadDocumentsAsync();
        var existing = documents.FirstOrDefault(d => d.Id == document.Id);

        if (existing != null)
        {
            documents.Remove(existing);
            document.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            document.CreatedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
        }

        documents.Add(document);
        await SaveDocumentsAsync(documents);
    }

    public async Task DeleteDocumentAsync(string id)
    {
        var documents = await LoadDocumentsAsync();
        var document = documents.FirstOrDefault(d => d.Id == id);

        if (document != null)
        {
            documents.Remove(document);
            await SaveDocumentsAsync(documents);
        }
    }

    public async Task<Dictionary<DocumentFolder, int>> GetFolderCountsAsync()
    {
        var documents = await LoadDocumentsAsync();
        var counts = new Dictionary<DocumentFolder, int>();

        foreach (DocumentFolder folder in Enum.GetValues<DocumentFolder>())
        {
            counts[folder] = documents.Count(d => d.Folder == folder);
        }

        return counts;
    }
}
