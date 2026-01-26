#if IOS
using Denly.Services;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;
using UserNotifications;

namespace Denly.Platforms.iOS;

/// <summary>
/// iOS implementation of push notifications using Apple Push Notification service (APNs).
/// </summary>
public class iOSPushNotificationService : PushNotificationService
{
    private TaskCompletionSource<bool>? _permissionCompletionSource;
    private NotificationCenterDelegate? _notificationDelegate;

    public iOSPushNotificationService(IAuthService authService, ILogger<iOSPushNotificationService> logger)
        : base(authService, logger)
    {
    }

    protected override string PlatformName => Models.DevicePlatform.iOS;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Set the delegate for handling notifications
            _notificationDelegate ??= new NotificationCenterDelegate(this);
            UNUserNotificationCenter.Current.Delegate = _notificationDelegate;

            // Check if we already have permission and a token
            var hasPermission = await HasPermissionAsync(cancellationToken);
            if (hasPermission)
            {
                // Register for remote notifications to get the token
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.RegisterForRemoteNotifications();
                });
            }

            _logger.LogDebug("[PushNotification] iOS APNs initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PushNotification] Failed to initialize iOS APNs");
        }
    }

    public override async Task<bool> RequestPermissionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _permissionCompletionSource = new TaskCompletionSource<bool>();

            var center = UNUserNotificationCenter.Current;
            var options = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;

            var (granted, error) = await center.RequestAuthorizationAsync(options);

            if (error != null)
            {
                _logger.LogWarning("[PushNotification] Permission request error: {Error}", error.LocalizedDescription);
            }

            OnPermissionStatusChanged(granted);

            if (granted)
            {
                // Register for remote notifications to get the token
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.RegisterForRemoteNotifications();
                });
            }

            return granted;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PushNotification] Failed to request permission");
            return false;
        }
    }

    public override async Task<bool> HasPermissionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var center = UNUserNotificationCenter.Current;
            var settings = await center.GetNotificationSettingsAsync();

            return settings.AuthorizationStatus == UNAuthorizationStatus.Authorized ||
                   settings.AuthorizationStatus == UNAuthorizationStatus.Provisional;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PushNotification] Failed to check permission status");
            return false;
        }
    }

    /// <summary>
    /// Called by AppDelegate when device token is received.
    /// </summary>
    internal async Task OnRegisteredForRemoteNotificationsAsync(NSData deviceToken)
    {
        var token = DeviceTokenToString(deviceToken);
        if (!string.IsNullOrEmpty(token))
        {
            _logger.LogDebug("[PushNotification] Received APNs device token");
            await RegisterTokenAsync(token);
        }
    }

    /// <summary>
    /// Called by AppDelegate when registration fails.
    /// </summary>
    internal void OnFailedToRegisterForRemoteNotifications(NSError error)
    {
        _logger.LogWarning("[PushNotification] Failed to register for remote notifications: {Error}",
            error.LocalizedDescription);
    }

    private void HandleWillPresentNotification(Action<UNNotificationPresentationOptions> completionHandler)
    {
        var options = UNNotificationPresentationOptions.Banner |
                     UNNotificationPresentationOptions.Sound |
                     UNNotificationPresentationOptions.Badge;

        completionHandler(options);
    }

    private void HandleDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler)
    {
        var userInfo = response.Notification.Request.Content.UserInfo;
        var data = ConvertUserInfoToDictionary(userInfo);
        var payload = ParsePayload(data);

        if (payload != null)
        {
            OnNotificationTapped(payload);
        }

        completionHandler();
    }

    private sealed class NotificationCenterDelegate : UNUserNotificationCenterDelegate
    {
        private readonly iOSPushNotificationService _service;

        public NotificationCenterDelegate(iOSPushNotificationService service)
        {
            _service = service;
        }

        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            _service.HandleWillPresentNotification(completionHandler);
        }

        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response,
            Action completionHandler)
        {
            _service.HandleDidReceiveNotificationResponse(response, completionHandler);
        }
    }

    private static string DeviceTokenToString(NSData deviceToken)
    {
        // Convert NSData to hex string (iOS 13+ format)
        var bytes = new byte[deviceToken.Length];
        System.Runtime.InteropServices.Marshal.Copy(deviceToken.Bytes, bytes, 0, (int)deviceToken.Length);
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private static Dictionary<string, string>? ConvertUserInfoToDictionary(NSDictionary? userInfo)
    {
        if (userInfo == null) return null;

        var dict = new Dictionary<string, string>();
        foreach (var key in userInfo.Keys)
        {
            var keyStr = key.ToString();
            var value = userInfo[key]?.ToString();
            if (!string.IsNullOrEmpty(keyStr) && value != null)
            {
                dict[keyStr] = value;
            }
        }
        return dict;
    }
}
#endif
