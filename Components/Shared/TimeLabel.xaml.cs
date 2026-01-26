using Microsoft.Maui.Controls;

namespace Denly.Components.Shared;

public partial class TimeLabel : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(TimeLabel), string.Empty);

    public TimeLabel()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
