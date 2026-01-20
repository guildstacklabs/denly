using Denly.Models;
using Microsoft.Extensions.Logging;

namespace Denly.Services;

public class SupabaseDocumentService : SupabaseServiceBase, IDocumentService
{
    private const string DocumentsBucket = "documents";

    private readonly IClock _clock;
    private readonly IStorageService _storageService;
    private readonly ILogger<SupabaseDocumentService> _logger;

    public SupabaseDocumentService(IDenService denService, IAuthService authService, IClock clock, IStorageService storageService, ILogger<SupabaseDocumentService> logger)
        : base(denService, authService)
    {
        _clock = clock;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<List<Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Document>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await GetClientOrThrow()
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
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Document>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var folderStr = folder.ToString().ToLowerInvariant();
            var response = await GetClientOrThrow()
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

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Document>();

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllDocumentsAsync(cancellationToken); // Return all if no query

        try
        {
            var result = await GetClientOrThrow()
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
            _logger.LogError(ex, "Failed to search documents");
            return new List<Document>();
        }
    }

    public async Task<List<Document>> GetRecentDocumentsAsync(int count = 3, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Document>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await GetClientOrThrow()
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
            return await GetClientOrThrow()
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
        await EnsureInitializedAsync();

        var denId = GetCurrentDenIdOrThrow();
        var userId = GetAuthenticatedUserIdOrThrow();
        var client = GetClientOrThrow();

        cancellationToken.ThrowIfCancellationRequested();

        document.DenId = denId;

        var existing = await GetDocumentByIdAsync(document.Id, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (existing != null)
        {
            await client
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
            document.UploadedBy = userId;
            document.CreatedAt = _clock.UtcNow;

            await client
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

        await GetClientOrThrow()
            .From<Document>()
            .Where(d => d.Id == id)
            .Delete();
    }

    public async Task<Dictionary<DocumentFolder, int>> GetFolderCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = new Dictionary<DocumentFolder, int>();

        foreach (DocumentFolder folder in Enum.GetValues<DocumentFolder>())
        {
            counts[folder] = 0;
        }

        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return counts;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await GetClientOrThrow()
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

        var denId = TryGetCurrentDenId();
        if (denId == null) return false;

        try
        {
            var result = await GetClientOrThrow()
                .From<Document>()
                .Select("id")
                .Filter("den_id", Supabase.Postgrest.Constants.Operator.Equals, denId)
                .Limit(1)
                .Get();

            return result.Models.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for documents");
            return false;
        }
    }

    public async Task<string> UploadDocumentAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var denId = GetCurrentDenIdOrThrow();

        try
        {
            var url = await _storageService.UploadAsync(DocumentsBucket, stream, fileName, denId, cancellationToken);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document");
            throw;
        }
    }
}
