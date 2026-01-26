using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Denly.Components.Shared;

public partial class ChildMark : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(ChildMark), string.Empty);

    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor), typeof(Color), typeof(ChildMark), Colors.Transparent);

    public ChildMark()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }
}
