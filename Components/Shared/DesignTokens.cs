using System.Globalization;
using Microsoft.Maui.Graphics;

namespace Denly.Components.Shared;

public static class DesignTokens
{
    public static class Colors
    {
        // Quiet Glass Palette
        public static readonly Color WarmBackground = new(0.976f, 0.976f, 0.976f, 1f); // #F9F9F9
        public static readonly Color DenShadow = new(0.102f, 0.102f, 0.102f, 1f);   // #1A1A1A
        public static readonly Color Teal = new(0.290f, 0.290f, 0.290f, 1f);        // #4A4A4A (Dark Grey)
        public static readonly Color Seafoam = new(0.878f, 0.878f, 0.878f, 1f);     // #E0E0E0 (Light Grey)
        public static readonly Color Gold = new(0.831f, 0.831f, 0.831f, 1f);        // #D4D4D4 (Silver)
        public static readonly Color Coral = new(0.878f, 0.478f, 0.373f, 1f);       // #E07A5F (Kept)
        public static readonly Color NookBackground = new(1f, 1f, 1f, 1f);          // #FFFFFF
        public static readonly Color BorderSoft = new(0f, 0f, 0f, 0.05f);           // Black @ 5%
        public static readonly Color SurfaceGlass = new(1f, 1f, 1f, 0.85f);         // White @ 85%
        public static readonly Color UserSienna = new(0.831f, 0.639f, 0.451f, 1f);  // #D4A373
        public static readonly Color UserLavender = new(0.663f, 0.667f, 0.737f, 1f); // #A9AABC
    }

    public static class Spacing
    {
        public const int Xs = 4;
        public const int Sm = 8;
        public const int Md = 12;
        public const int Lg = 16;
        public const int Xl = 20;
        public const int Xxl = 24;
        public const int Xxxl = 32;
        public const int Jumbo = 40;
    }

    public static class Dimensions
    {
        public const int RadiusNook = 24;
        public const int RadiusMd = 12;
        public const int RadiusPebble = 999;
        public const int TapMin = 44;
        public const int ChipSize = 28;
    }

    public static class Typography
    {
        public const string FontHeading = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
        public const string FontBody = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

        public static double SizeXs => 12;
        public static double SizeSm => 14;
        public static double SizeMd => 16;
        public static double SizeLg => 18;
        public static double SizeXl => 20;
        public static double Size2xl => 24;

        public const int WeightRegular = 400;
        public const int WeightMedium = 500;
        public const int WeightSemiBold = 600;
        public const int WeightBold = 700;
    }

    public static class Shadows
    {
        public const string Nook = "0 4px 20px rgba(0, 0, 0, 0.03)";
        public const string Pebble = "0 4px 12px rgba(0, 0, 0, 0.08)";
        public const string Glow = "0 0 16px rgba(0, 0, 0, 0.1)";
    }

    public static class Opacity
    {
        public const double Muted = 0.6;
        public const double Soft = 0.85;
    }

    public static string CssRgba(Color color)
    {
        var r = (int)Math.Round(color.Red * 255);
        var g = (int)Math.Round(color.Green * 255);
        var b = (int)Math.Round(color.Blue * 255);
        var a = color.Alpha.ToString("0.###", CultureInfo.InvariantCulture);
        return $"rgba({r}, {g}, {b}, {a})";
    }
}