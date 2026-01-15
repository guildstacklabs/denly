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
                .Select("id, den_id, child_id, title, category, file_url, uploaded_by, created_at")
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
                .Select("id, den_id, child_id, title, category, file_url, uploaded_by, created_at")
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
        await EnsureInitializedAsync();
        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Document>();

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllDocumentsAsync(cancellationToken); // Return all if no query

        try
        {
            var result = await SupabaseClient!
               .From<Document>()
               .Select("id, den_id, title, category, file_url, created_at, uploaded_by")
               .Filter("den_id", Supabase.Postgrest.Constants.Operator.Equals, denId)
               .Filter("title", Supabase.Postgrest.Constants.Operator.ILike, $"%{searchTerm}%")
               .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
               .Get();

            return result.Models;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentService] Error searching documents: {ex.Message}");
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
                .Select("id, den_id, child_id, title, category, file_url, uploaded_by, created_at")
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
                .Select("id, den_id, child_id, title, category, file_url, uploaded_by, created_at")
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
                .Select("id, den_id, child_id, title, category, file_url, uploaded_by, created_at")
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

    public async Task<bool> HasDocumentsAsync()
    {
        await EnsureInitializedAsync();
        var denId = DenService.GetCurrentDenId();
        if (denId == null) return false;

        try
        {
            var result = await SupabaseClient!
                .From<Document>()
                .Select("id")
                .Filter("den_id", Supabase.Postgrest.Constants.Operator.Equals, denId)
                .Limit(1)
                .Get();

            return result.Models.Count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentService] Error checking for documents: {ex.Message}");
            return false;
        }
    }

    public async Task<string> UploadDocumentAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            throw new InvalidOperationException("No den selected for document upload.");
        }

        try
        {
            var url = await _storageService.UploadAsync(DocumentsBucket, stream, fileName, denId, cancellationToken);
            return url;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentService] Error uploading document: {ex.Message}");
            throw;
        }
    }
}
