namespace Denly.Services;

/// <summary>
/// Supabase implementation of file storage operations.
/// </summary>
public class SupabaseStorageService : IStorageService
{
    private readonly IAuthService _authService;

    public SupabaseStorageService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<string> UploadAsync(string bucket, Stream stream, string fileName, string? pathPrefix = null, CancellationToken cancellationToken = default)
    {
        var client = _authService.GetSupabaseClient();
        if (client == null)
        {
            throw new InvalidOperationException("Supabase client not initialized");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Generate unique file name
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";

        if (!string.IsNullOrEmpty(pathPrefix))
        {
            uniqueName = $"{pathPrefix}/{uniqueName}";
        }

        // Read stream to byte array
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        // Upload to storage
        await client.Storage
            .From(bucket)
            .Upload(bytes, uniqueName);

        // Return the public URL
        return client.Storage
            .From(bucket)
            .GetPublicUrl(uniqueName);
    }

    public async Task DeleteAsync(string bucket, string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        var client = _authService.GetSupabaseClient();
        if (client == null)
        {
            throw new InvalidOperationException("Supabase client not initialized");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Extract path from URL
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath.Replace($"/storage/v1/object/public/{bucket}/", "");

            await client.Storage
                .From(bucket)
                .Remove(new List<string> { path });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StorageService] Error deleting file: {ex.Message}");
        }
    }

    public string GetPublicUrl(string bucket, string path)
    {
        var client = _authService.GetSupabaseClient();
        if (client == null)
        {
            throw new InvalidOperationException("Supabase client not initialized");
        }

        return client.Storage
            .From(bucket)
            .GetPublicUrl(path);
    }
}
