using Denly.Models;
using Microsoft.Extensions.Options;
using Supabase;

namespace Denly.Services;

public class SupabaseDocumentService : IDocumentService
{
    private const string DocumentsBucket = "documents";

    private readonly IDenService _denService;
    private readonly IAuthService _authService;
    private readonly DenlyOptions _options;
    private readonly IClock _clock;
    private Supabase.Client? _supabase;
    private bool _isInitialized;

    public SupabaseDocumentService(IDenService denService, IAuthService authService, IOptions<DenlyOptions> options, IClock clock)
    {
        _denService = denService;
        _authService = authService;
        _options = options.Value;
        _clock = clock;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        if (string.IsNullOrWhiteSpace(_options.SupabaseUrl) || string.IsNullOrWhiteSpace(_options.SupabaseAnonKey))
            throw new InvalidOperationException("Supabase configuration is missing.");

        _supabase = new Supabase.Client(_options.SupabaseUrl, _options.SupabaseAnonKey, options);
        await _supabase.InitializeAsync();
        _isInitialized = true;
    }

    public async Task<List<Document>> GetAllDocumentsAsync()
    {
        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        await EnsureInitializedAsync();

        try
        {
            var response = await _supabase!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Get();

            return response.Models
                .OrderBy(d => d.Folder)
                .ThenBy(d => d.Title)
                .ToList();
        }
        catch
        {
            return new List<Document>();
        }
    }

    public async Task<List<Document>> GetDocumentsByFolderAsync(DocumentFolder folder)
    {
        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        await EnsureInitializedAsync();

        try
        {
            var folderStr = folder.ToString().ToLowerInvariant();
            var response = await _supabase!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Filter("category", Supabase.Postgrest.Constants.Operator.Equals, folderStr)
                .Get();

            return response.Models.OrderBy(d => d.Title).ToList();
        }
        catch
        {
            return new List<Document>();
        }
    }

    public async Task<List<Document>> SearchDocumentsAsync(string searchTerm)
    {
        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        await EnsureInitializedAsync();

        try
        {
            var response = await _supabase!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Get();

            var documents = response.Models;

            if (string.IsNullOrWhiteSpace(searchTerm))
                return documents.OrderBy(d => d.Folder).ThenBy(d => d.Title).ToList();

            var term = searchTerm.ToLowerInvariant();
            return documents
                .Where(d => d.Title.ToLowerInvariant().Contains(term) ||
                           (d.FileName?.ToLowerInvariant().Contains(term) ?? false))
                .OrderBy(d => d.Folder)
                .ThenBy(d => d.Title)
                .ToList();
        }
        catch
        {
            return new List<Document>();
        }
    }

    public async Task<List<Document>> GetRecentDocumentsAsync(int count = 3)
    {
        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        await EnsureInitializedAsync();

        try
        {
            var response = await _supabase!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(count)
                .Get();

            return response.Models;
        }
        catch
        {
            return new List<Document>();
        }
    }

    public async Task<Document?> GetDocumentByIdAsync(string id)
    {
        await EnsureInitializedAsync();

        try
        {
            return await _supabase!
                .From<Document>()
                .Where(d => d.Id == id)
                .Single();
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveDocumentAsync(Document document)
    {
        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return;

        var user = await _authService.GetCurrentUserAsync();
        if (user == null) return;

        await EnsureInitializedAsync();

        document.DenId = denId;

        var existing = await GetDocumentByIdAsync(document.Id);

        if (existing != null)
        {
            await _supabase!
                .From<Document>()
                .Where(d => d.Id == document.Id)
                .Set(d => d.Title, document.Title)
                .Set(d => d.CategoryString, document.CategoryString)
                .Set(d => d.FileUrl!, document.FileUrl)
                .Set(d => d.ChildId!, document.ChildId)
                .Update();
        }
        else
        {
            document.UploadedBy = user.Id;
            document.CreatedAt = _clock.UtcNow;

            await _supabase!
                .From<Document>()
                .Insert(document);
        }
    }

    public async Task DeleteDocumentAsync(string id)
    {
        await EnsureInitializedAsync();

        // Get document to delete file if exists
        var document = await GetDocumentByIdAsync(id);
        if (document?.FileUrl != null)
        {
            try
            {
                // Extract path from URL
                var uri = new Uri(document.FileUrl);
                var path = uri.AbsolutePath.Replace($"/storage/v1/object/public/{DocumentsBucket}/", "");

                await _supabase!.Storage
                    .From(DocumentsBucket)
                    .Remove(new List<string> { path });
            }
            catch
            {
                // Best effort deletion
            }
        }

        await _supabase!
            .From<Document>()
            .Where(d => d.Id == id)
            .Delete();
    }

    public async Task<Dictionary<DocumentFolder, int>> GetFolderCountsAsync()
    {
        var denId = _denService.GetCurrentDenId();
        var counts = new Dictionary<DocumentFolder, int>();

        foreach (DocumentFolder folder in Enum.GetValues<DocumentFolder>())
        {
            counts[folder] = 0;
        }

        if (string.IsNullOrEmpty(denId)) return counts;

        await EnsureInitializedAsync();

        try
        {
            var response = await _supabase!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Get();

            foreach (var doc in response.Models)
            {
                counts[doc.Folder]++;
            }
        }
        catch
        {
            // Return zeros on error
        }

        return counts;
    }
}
