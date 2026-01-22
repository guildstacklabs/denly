﻿using Android.App;
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

        var window = Window;
        if (window == null)
        {
            return;
        }

        // Enable edge-to-edge display
        WindowCompat.SetDecorFitsSystemWindows(window, false);

        // Allow content to render in display cutout area
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            var attributes = window.Attributes;
            if (attributes != null)
            {
                attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                window.Attributes = attributes;
            }
        }

        // Set up insets listener to inject safe area values into CSS
        var rootView = window.DecorView?.RootView;
        if (rootView == null)
        {
            return;
        }
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
            var safeInsets = (insets ?? new WindowInsetsCompat.Builder().Build())!;
            if (v == null)
            {
                return safeInsets;
            }

            var systemBars = safeInsets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var displayCutout = safeInsets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

            // Use the maximum of system bars and display cutout
            var top = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
            var bottom = Math.Max(systemBars?.Bottom ?? 0, displayCutout?.Bottom ?? 0);
            var left = Math.Max(systemBars?.Left ?? 0, displayCutout?.Left ?? 0);
            var right = Math.Max(systemBars?.Right ?? 0, displayCutout?.Right ?? 0);

            // Convert pixels to CSS pixels (account for density)
            var resources = _activity.Resources;
            var displayMetrics = resources?.DisplayMetrics;
            if (displayMetrics == null)
            {
                return ViewCompat.OnApplyWindowInsets(v, safeInsets) ?? safeInsets;
            }
            var density = displayMetrics.Density;
            var topDp = (int)(top / density);
            var bottomDp = (int)(bottom / density);
            var leftDp = (int)(left / density);
            var rightDp = (int)(right / density);

            // Store values for CSS injection
            SafeAreaInsets.Top = topDp;
            SafeAreaInsets.Bottom = bottomDp;
            SafeAreaInsets.Left = leftDp;
            SafeAreaInsets.Right = rightDp;

            return ViewCompat.OnApplyWindowInsets(v, safeInsets) ?? safeInsets;
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
