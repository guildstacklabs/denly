using Denly.Models;
using Microsoft.Extensions.Logging;

namespace Denly.Services;

/// <summary>
/// Shared push notification service that handles token registration with the backend.
/// Platform-specific implementations inherit from this and add native registration.
/// </summary>
public abstract class PushNotificationService : IPushNotificationService
{
    protected readonly IAuthService _authService;
    protected readonly ILogger _logger;
    protected string? _currentToken;

    public event EventHandler<bool>? PermissionStatusChanged;
    public event EventHandler<NotificationPayload>? NotificationTapped;

    protected PushNotificationService(IAuthService authService, ILogger logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Platform identifier for database storage.
    /// </summary>
    protected abstract string PlatformName { get; }

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> RequestPermissionAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> HasPermissionAsync(CancellationToken cancellationToken = default);

    public string? GetCurrentToken() => _currentToken;

    public async Task RegisterTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[PushNotification] Attempted to register empty token");
            return;
        }

        _currentToken = token;

        var client = _authService.GetSupabaseClient();
        if (client?.Auth.CurrentUser == null)
        {
            _logger.LogWarning("[PushNotification] No authenticated user, cannot register token");
            return;
        }

        var userId = client.Auth.CurrentUser.Id;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("[PushNotification] User ID is empty, cannot register token");
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Upsert token (insert or update on conflict)
            var deviceToken = new DeviceToken
            {
                UserId = userId,
                Platform = PlatformName,
                Token = token
            };

            await client.From<DeviceToken>()
                .Upsert(deviceToken);

            _logger.LogDebug("[PushNotification] Token registered for user {UserId} on {Platform}",
                userId.Substring(0, 8), PlatformName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PushNotification] Failed to register token");
        }
    }

    public async Task UnregisterTokenAsync(CancellationToken cancellationToken = default)
    {
        var client = _authService.GetSupabaseClient();
        if (client?.Auth.CurrentUser == null)
        {
            _logger.LogDebug("[PushNotification] No authenticated user, nothing to unregister");
            _currentToken = null;
            return;
        }

        var userId = client.Auth.CurrentUser.Id;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await client.From<DeviceToken>()
                .Where(t => t.UserId == userId && t.Platform == PlatformName)
                .Delete();

            _logger.LogDebug("[PushNotification] Token unregistered for user on {Platform}", PlatformName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PushNotification] Failed to unregister token");
        }
        finally
        {
            _currentToken = null;
        }
    }

    /// <summary>
    /// Raises the PermissionStatusChanged event.
    /// </summary>
    protected void OnPermissionStatusChanged(bool isGranted)
    {
        PermissionStatusChanged?.Invoke(this, isGranted);
    }

    /// <summary>
    /// Raises the NotificationTapped event for deep linking.
    /// Call this from platform-specific code when a notification is tapped.
    /// </summary>
    protected void OnNotificationTapped(NotificationPayload payload)
    {
        NotificationTapped?.Invoke(this, payload);
    }

    /// <summary>
    /// Parses a notification payload from the data dictionary.
    /// </summary>
    protected static NotificationPayload? ParsePayload(IDictionary<string, string>? data)
    {
        if (data == null) return null;

        if (!data.TryGetValue("type", out var typeStr) ||
            !data.TryGetValue("den_id", out var denId))
        {
            return null;
        }

        if (!Enum.TryParse<NotificationType>(typeStr, ignoreCase: true, out var type))
        {
            return null;
        }

        data.TryGetValue("item_id", out var itemId);

        return new NotificationPayload(type, denId, itemId);
    }
}
