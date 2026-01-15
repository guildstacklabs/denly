using Denly.Models;

namespace Denly.Services;

public interface IDocumentService
{
    Task<List<Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsByFolderAsync(DocumentFolder folder, CancellationToken cancellationToken = default);
    Task<List<Document>> SearchDocumentsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<Document>> GetRecentDocumentsAsync(int count = 3, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentByIdAsync(string id, CancellationToken cancellationToken = default);
    Task SaveDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string id, CancellationToken cancellationToken = default);
    Task<Dictionary<DocumentFolder, int>> GetFolderCountsAsync(CancellationToken cancellationToken = default);
}
