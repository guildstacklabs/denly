namespace Denly.Services;

/// <summary>
/// Interface for file storage operations.
/// Abstracts storage provider details from business logic.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file to the specified bucket.
    /// </summary>
    /// <param name="bucket">The storage bucket name (e.g., "receipts", "documents")</param>
    /// <param name="stream">The file content stream</param>
    /// <param name="fileName">The original file name</param>
    /// <param name="pathPrefix">Optional path prefix within the bucket (e.g., den ID)</param>
    /// <returns>The public URL of the uploaded file</returns>
    Task<string> UploadAsync(string bucket, Stream stream, string fileName, string? pathPrefix = null);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    /// <param name="bucket">The storage bucket name</param>
    /// <param name="fileUrl">The public URL of the file to delete</param>
    Task DeleteAsync(string bucket, string fileUrl);

    /// <summary>
    /// Gets the public URL for a file in storage.
    /// </summary>
    /// <param name="bucket">The storage bucket name</param>
    /// <param name="path">The file path within the bucket</param>
    /// <returns>The public URL</returns>
    string GetPublicUrl(string bucket, string path);
}
