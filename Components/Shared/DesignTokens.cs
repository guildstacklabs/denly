using System.Globalization;
using Microsoft.Maui.Graphics;

namespace Denly.Components.Shared;

public static class DesignTokens
{
    public static class Colors
    {
        public static readonly Color WarmBackground = new(1f, 0.976f, 0.941f, 1f); // #FFF9F0
        public static readonly Color DenShadow = new(0.243f, 0.259f, 0.294f, 1f);   // #3E424B
        public static readonly Color Teal = new(0.239f, 0.545f, 0.545f, 1f);        // #3D8B8B
        public static readonly Color Seafoam = new(0.506f, 0.698f, 0.604f, 1f);     // #81B29A
        public static readonly Color Gold = new(0.949f, 0.800f, 0.561f, 1f);        // #F2CC8F
        public static readonly Color Coral = new(0.878f, 0.478f, 0.373f, 1f);       // #E07A5F
        public static readonly Color NookBackground = new(0.992f, 0.988f, 0.973f, 1f); // #FDFCF8
        public static readonly Color BorderSoft = new(0.243f, 0.259f, 0.294f, 0.08f);   // DenShadow @ 8%
        public static readonly Color SurfaceGlass = new(0.992f, 0.988f, 0.973f, 0.70f);
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
        public const string FontHeading = "Nunito";
        public const string FontBody = "Inter";

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
        public const string Nook = "inset 0 2px 6px rgba(242, 204, 143, 0.15), 0 4px 10px rgba(62, 66, 75, 0.05)";
        public const string Pebble = "0 4px 12px rgba(61, 139, 139, 0.3)";
        public const string Glow = "0 0 16px rgba(242, 204, 143, 0.45)";
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
