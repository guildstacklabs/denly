#if ANDROID
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Denly.Services;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;

namespace Denly.Platforms.Android;

/// <summary>
/// Android implementation of push notifications using Firebase Cloud Messaging (FCM).
/// </summary>
public class AndroidPushNotificationService : PushNotificationService
{
    private const string ChannelId = "denly_notifications";
    private const string ChannelName = "Denly Notifications";

    public AndroidPushNotificationService(IAuthService authService, ILogger<AndroidPushNotificationService> logger)
        : base(authService, logger)
    {
    }

    protected override string PlatformName => Models.DevicePlatform.Android;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            CreateNotificationChannel();

            // Check if we already have a token
            var existingToken = await GetFcmTokenAsync(cancellationToken);
            if (!string.IsNullOrEmpty(existingToken))
            {
                await RegisterTokenAsync(existingToken, cancellationToken);
            }

            _logger.LogDebug("[PushNotification] Android FCM initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PushNotification] Failed to initialize Android FCM");
        }
    }

    public override async Task<bool> RequestPermissionAsync(CancellationToken cancellationToken = default)
    {
        // Android 13+ (API 33+) requires POST_NOTIFICATIONS permission
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                _logger.LogWarning("[PushNotification] No current activity, cannot request permission");
                return false;
            }

            var status = ContextCompat.CheckSelfPermission(activity, Manifest.Permission.PostNotifications);
            if (status == Permission.Granted)
            {
                OnPermissionStatusChanged(true);
                return true;
            }

            // Request permission
            ActivityCompat.RequestPermissions(activity, new[] { Manifest.Permission.PostNotifications }, 1001);

            // Note: In a real implementation, you'd handle the result in MainActivity.OnRequestPermissionsResult
            // For now, we check again after a delay
            await Task.Delay(500, cancellationToken);

            status = ContextCompat.CheckSelfPermission(activity, Manifest.Permission.PostNotifications);
            var granted = status == Permission.Granted;
            OnPermissionStatusChanged(granted);

            if (granted)
            {
                var token = await GetFcmTokenAsync(cancellationToken);
                if (!string.IsNullOrEmpty(token))
                {
                    await RegisterTokenAsync(token, cancellationToken);
                }
            }

            return granted;
        }

        // Pre-Android 13: notifications always allowed
        OnPermissionStatusChanged(true);

        var fcmToken = await GetFcmTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(fcmToken))
        {
            await RegisterTokenAsync(fcmToken, cancellationToken);
        }

        return true;
    }

    public override Task<bool> HasPermissionAsync(CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var activity = Platform.CurrentActivity;
            if (activity == null) return Task.FromResult(false);

            var status = ContextCompat.CheckSelfPermission(activity, Manifest.Permission.PostNotifications);
            return Task.FromResult(status == Permission.Granted);
        }

        // Pre-Android 13: notifications always allowed
        return Task.FromResult(true);
    }

    private Task<string?> GetFcmTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // If we already have a token cached, return it
            if (!string.IsNullOrEmpty(_currentToken))
            {
                return Task.FromResult<string?>(_currentToken);
            }

            // The token will be delivered via OnNewToken callback in DenlyFirebaseMessagingService.
            // For initialization, we return null and let the callback handle registration.
            // FirebaseMessaging will call OnNewToken when the token is available.
            _logger.LogDebug("[PushNotification] No cached token, waiting for OnNewToken callback");
            return Task.FromResult<string?>(null);
        }
        catch (System.OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PushNotification] Failed to get FCM token");
            return Task.FromResult<string?>(null);
        }
    }

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var context = Platform.AppContext;
        var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (notificationManager == null) return;

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Default)
        {
            Description = "Notifications from Denly co-parenting app"
        };

        notificationManager.CreateNotificationChannel(channel);
    }

    /// <summary>
    /// Called by DenlyFirebaseMessagingService when a new token is received.
    /// </summary>
    internal void OnNewToken(string token)
    {
        _currentToken = token;

        // Register token in background (fire and forget since we can't await here)
        _ = RegisterTokenAsync(token);
    }

    /// <summary>
    /// Called by DenlyFirebaseMessagingService when a message is received while app is in foreground.
    /// </summary>
    internal void OnMessageReceived(RemoteMessage message)
    {
        var data = message.Data;
        var payload = ParsePayload(data);

        if (payload != null)
        {
            // Show local notification since app is in foreground
            ShowLocalNotification(message.GetNotification()?.Title, message.GetNotification()?.Body, data);
        }
    }

    /// <summary>
    /// Called when user taps a notification.
    /// </summary>
    internal void OnNotificationOpened(IDictionary<string, string>? data)
    {
        var payload = ParsePayload(data);
        if (payload != null)
        {
            OnNotificationTapped(payload);
        }
    }

    private void ShowLocalNotification(string? title, string? body, IDictionary<string, string>? data)
    {
        if (Platform.AppContext is not { } context) return;

        var intent = new Intent(context, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

        if (data != null)
        {
            foreach (var kvp in data)
            {
                intent.PutExtra(kvp.Key, kvp.Value);
            }
        }

        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        var pendingIntent = PendingIntent.GetActivity(context, 0, intent, pendingIntentFlags);

        var builder = new NotificationCompat.Builder(context, ChannelId);
        builder.SetAutoCancel(true);
        builder.SetContentTitle(title ?? "Denly");
        builder.SetContentText(body ?? "");
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentIntent(pendingIntent);
        var notification = builder.Build();

        if (notification == null) return;

        var notificationManager = NotificationManagerCompat.From(context);
        if (notificationManager == null) return;

        // Check permission before showing notification (Android 13+)
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) != Permission.Granted)
            {
                return;
            }
        }

        notificationManager.Notify(DateTime.Now.Millisecond, notification);
    }
}

/// <summary>
/// Firebase Messaging Service to handle FCM events.
/// </summary>
[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class DenlyFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);

        // Get the service from DI and notify it
        var serviceProvider = IPlatformApplication.Current?.Services;
        if (serviceProvider?.GetService<IPushNotificationService>() is AndroidPushNotificationService pushService)
        {
            pushService.OnNewToken(token);
        }
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        var serviceProvider = IPlatformApplication.Current?.Services;
        if (serviceProvider?.GetService<IPushNotificationService>() is AndroidPushNotificationService pushService)
        {
            pushService.OnMessageReceived(message);
        }
    }
}
#endif
