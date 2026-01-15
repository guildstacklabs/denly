using SkiaSharp;
using System.IO;

namespace Denly.Services;

/// <summary>
/// Supabase implementation of file storage operations.
/// </summary>
public class SupabaseStorageService : IStorageService
{
    private readonly IAuthService _authService;
    private const int MaxImageDimension = 1024;
    private const int JpegQuality = 80;

    public SupabaseStorageService(IAuthService authService)
    {
        _authService = authService;
    }

    private Stream CompressImageIfNeeded(Stream input, string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
        {
            return input;
        }

        // We need a seekable stream for SkiaSharp to decode. If the input isn't, copy it.
        var memoryStream = new MemoryStream();
        input.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var original = SKBitmap.Decode(memoryStream);
        if (original == null)
        {
            memoryStream.Position = 0;
            return memoryStream; // Not a valid image, upload original
        }

        var maxDim = Math.Max(original.Width, original.Height);
        if (maxDim <= MaxImageDimension)
        {
            memoryStream.Position = 0;
            return memoryStream; // Already small enough
        }

        var scale = (float)MaxImageDimension / maxDim;
        var newWidth = (int)(original.Width * scale);
        var newHeight = (int)(original.Height * scale);

        using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
        if (resized == null)
        {
            memoryStream.Position = 0;
            return memoryStream; // Resize failed, upload original
        }
        using var image = SKImage.FromBitmap(resized);
        
        // Encode to JPEG for best compression, even for PNGs.
        var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return data.AsStream();
    }


    public async Task<string> UploadAsync(string bucket, Stream stream, string fileName, string? pathPrefix = null, CancellationToken cancellationToken = default)
    {
        var client = _authService.GetSupabaseClient();
        if (client == null)
        {
            throw new InvalidOperationException("Supabase client not initialized");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Compress the stream if it's an image
        using var compressedStream = CompressImageIfNeeded(stream, fileName);

        // Generate unique file name, preferring jpg for compressed images
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        var isCompressed = extension is ".jpg" or ".jpeg" or ".png";
        var finalExtension = isCompressed ? ".jpg" : extension;
        var uniqueName = $"{Guid.NewGuid()}{finalExtension}";

        if (!string.IsNullOrEmpty(pathPrefix))
        {
            uniqueName = $"{pathPrefix}/{uniqueName}";
        }

        // Read stream to byte array
        using var memoryStream = new MemoryStream();
        await compressedStream.CopyToAsync(memoryStream, cancellationToken);
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
