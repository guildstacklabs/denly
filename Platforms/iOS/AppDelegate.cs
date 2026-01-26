using Denly.Platforms.iOS;
using Denly.Services;
using Foundation;
using Microsoft.Maui;
using UIKit;

namespace Denly;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	[Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
	public void DidRegisterForRemoteNotifications(UIApplication application, NSData deviceToken)
	{
		// Get the push notification service and register the token
		var serviceProvider = IPlatformApplication.Current?.Services;
		if (serviceProvider?.GetService<IPushNotificationService>() is iOSPushNotificationService pushService)
		{
			_ = pushService.OnRegisteredForRemoteNotificationsAsync(deviceToken);
		}
	}

	[Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
	public void DidFailToRegisterForRemoteNotifications(UIApplication application, NSError error)
	{
		var serviceProvider = IPlatformApplication.Current?.Services;
		if (serviceProvider?.GetService<IPushNotificationService>() is iOSPushNotificationService pushService)
		{
			pushService.OnFailedToRegisterForRemoteNotifications(error);
		}
	}
}
