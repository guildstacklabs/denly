using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Denly.Components.Shared;

public partial class DenText : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(DenText),
        string.Empty);

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily),
        typeof(string),
        typeof(DenText),
        DesignTokens.Typography.FontBody);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize),
        typeof(double),
        typeof(DenText),
        (double)DesignTokens.Typography.SizeMd);

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor),
        typeof(Color),
        typeof(DenText),
        DesignTokens.Colors.DenShadow);

    public DenText()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }
}
