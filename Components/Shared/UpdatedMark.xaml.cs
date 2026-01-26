using Microsoft.Maui.Controls;

namespace Denly.Components.Shared;

public partial class UpdatedMark : ContentView
{
    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(UpdatedMark), false);

    public UpdatedMark()
    {
        InitializeComponent();
    }

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }
}
