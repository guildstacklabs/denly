namespace Denly.Services;

/// <summary>
/// Types of notifications supported by the app.
/// </summary>
public enum NotificationType
{
    ExpenseAdded,
    EventCreated,
    EventUpdated,
    SettlementRequested,
    SettlementConfirmed
}

/// <summary>
/// Data payload for a push notification.
/// Contains IDs only - no PII. Details fetched in-app when notification is tapped.
/// </summary>
public record NotificationPayload(
    NotificationType Type,
    string DenId,
    string? ItemId = null
);

/// <summary>
/// Interface for push notification operations.
/// Platform-specific implementations handle registration with APNs (iOS) or FCM (Android).
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Event raised when push permission status changes.
    /// </summary>
    event EventHandler<bool>? PermissionStatusChanged;

    /// <summary>
    /// Event raised when a notification is tapped (for deep linking).
    /// </summary>
    event EventHandler<NotificationPayload>? NotificationTapped;

    /// <summary>
    /// Initializes the push notification service.
    /// Call this after user authentication to ensure proper token association.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests permission to send push notifications.
    /// Should only be called on user action, not automatically on app launch.
    /// </summary>
    /// <returns>True if permission was granted, false otherwise.</returns>
    Task<bool> RequestPermissionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if push notification permission has been granted.
    /// </summary>
    Task<bool> HasPermissionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers the current device token with the backend.
    /// Called automatically when permission is granted and token is received.
    /// </summary>
    Task RegisterTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters the device token from the backend.
    /// Call this when user signs out.
    /// </summary>
    Task UnregisterTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current device token if available.
    /// </summary>
    string? GetCurrentToken();
}
