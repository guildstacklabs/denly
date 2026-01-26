using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Denly.Components.Shared;

public partial class DenChip : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(DenChip), string.Empty);

    public static readonly BindableProperty ChipColorProperty = BindableProperty.Create(
        nameof(ChipColor), typeof(Color), typeof(DenChip), DesignTokens.Colors.Seafoam);

    public DenChip()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color ChipColor
    {
        get => (Color)GetValue(ChipColorProperty);
        set => SetValue(ChipColorProperty, value);
    }
}
