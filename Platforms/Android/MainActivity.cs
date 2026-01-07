using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace Denly;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Enable edge-to-edge display
        WindowCompat.SetDecorFitsSystemWindows(Window!, false);

        // Allow content to render in display cutout area
        if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
        {
            Window!.Attributes!.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
        }

        // Set up insets listener to inject safe area values into CSS
        var rootView = Window!.DecorView.RootView;
        ViewCompat.SetOnApplyWindowInsetsListener(rootView, new SafeAreaInsetsListener(this));
    }

    private class SafeAreaInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        private readonly MainActivity _activity;

        public SafeAreaInsetsListener(MainActivity activity)
        {
            _activity = activity;
        }

        public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View? v, WindowInsetsCompat? insets)
        {
            if (insets == null || v == null)
                return insets ?? new WindowInsetsCompat.Builder().Build();

            var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

            // Use the maximum of system bars and display cutout
            var top = Math.Max(systemBars.Top, displayCutout.Top);
            var bottom = Math.Max(systemBars.Bottom, displayCutout.Bottom);
            var left = Math.Max(systemBars.Left, displayCutout.Left);
            var right = Math.Max(systemBars.Right, displayCutout.Right);

            // Convert pixels to CSS pixels (account for density)
            var density = _activity.Resources!.DisplayMetrics!.Density;
            var topDp = (int)(top / density);
            var bottomDp = (int)(bottom / density);
            var leftDp = (int)(left / density);
            var rightDp = (int)(right / density);

            // Store values for CSS injection
            SafeAreaInsets.Top = topDp;
            SafeAreaInsets.Bottom = bottomDp;
            SafeAreaInsets.Left = leftDp;
            SafeAreaInsets.Right = rightDp;

            return ViewCompat.OnApplyWindowInsets(v, insets);
        }
    }
}

public static class SafeAreaInsets
{
    public static int Top { get; set; }
    public static int Bottom { get; set; }
    public static int Left { get; set; }
    public static int Right { get; set; }
}
