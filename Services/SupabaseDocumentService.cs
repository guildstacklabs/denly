using Denly.Models;

namespace Denly.Services;

public class SupabaseDocumentService : SupabaseServiceBase, IDocumentService
{
    private const string DocumentsBucket = "documents";

    private readonly IClock _clock;
    private readonly IStorageService _storageService;

    public SupabaseDocumentService(IDenService denService, IAuthService authService, IClock clock, IStorageService storageService)
        : base(denService, authService)
    {
        _clock = clock;
        _storageService = storageService;
    }

    public async Task<List<Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await SupabaseClient!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Get();

            return response.Models
                .OrderBy(d => d.Folder)
                .ThenBy(d => d.Title)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new List<Document>();
        }
    }

    public async Task<List<Document>> GetDocumentsByFolderAsync(DocumentFolder folder, CancellationToken cancellationToken = default)
    {
        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var folderStr = folder.ToString().ToLowerInvariant();
            var response = await SupabaseClient!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Filter("category", Supabase.Postgrest.Constants.Operator.Equals, folderStr)
                .Get();

            return response.Models.OrderBy(d => d.Title).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new List<Document>();
        }
    }

    public async Task<List<Document>> SearchDocumentsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await SupabaseClient!
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new List<Document>();
        }
    }

    public async Task<List<Document>> GetRecentDocumentsAsync(int count = 3, CancellationToken cancellationToken = default)
    {
        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await SupabaseClient!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(count)
                .Get();

            return response.Models;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new List<Document>();
        }
    }

    public async Task<Document?> GetDocumentByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await SupabaseClient!
                .From<Document>()
                .Where(d => d.Id == id)
                .Single();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return;

        var user = await AuthService.GetCurrentUserAsync();
        if (user == null) return;

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        document.DenId = denId;

        var existing = await GetDocumentByIdAsync(document.Id, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (existing != null)
        {
            await SupabaseClient!
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

            await SupabaseClient!
                .From<Document>()
                .Insert(document);
        }
    }

    public async Task DeleteDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        // Get document to delete file if exists
        var document = await GetDocumentByIdAsync(id, cancellationToken);
        if (document?.FileUrl != null)
        {
            await _storageService.DeleteAsync(DocumentsBucket, document.FileUrl, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        await SupabaseClient!
            .From<Document>()
            .Where(d => d.Id == id)
            .Delete();
    }

    public async Task<Dictionary<DocumentFolder, int>> GetFolderCountsAsync(CancellationToken cancellationToken = default)
    {
        var denId = DenService.GetCurrentDenId();
        var counts = new Dictionary<DocumentFolder, int>();

        foreach (DocumentFolder folder in Enum.GetValues<DocumentFolder>())
        {
            counts[folder] = 0;
        }

        if (string.IsNullOrEmpty(denId)) return counts;

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await SupabaseClient!
                .From<Document>()
                .Where(d => d.DenId == denId)
                .Get();

            foreach (var doc in response.Models)
            {
                counts[doc.Folder]++;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Return zeros on error
        }

        return counts;
    }
}
