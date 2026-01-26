using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Denly.Components.Shared;

public partial class DenSurface : ContentView
{
    public static readonly BindableProperty SurfaceColorProperty = BindableProperty.Create(
        nameof(SurfaceColor),
        typeof(Color),
        typeof(DenSurface),
        DesignTokens.Colors.NookBackground);

    public DenSurface()
    {
        InitializeComponent();
        Padding = new Thickness(DesignTokens.Spacing.Xl);
    }

    public Color SurfaceColor
    {
        get => (Color)GetValue(SurfaceColorProperty);
        set => SetValue(SurfaceColorProperty, value);
    }
}
