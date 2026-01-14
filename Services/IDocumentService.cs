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
