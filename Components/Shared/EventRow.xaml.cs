using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Denly.Components.Shared;

public partial class EventRow : ContentView
{
    public static readonly BindableProperty TimeTextProperty = BindableProperty.Create(
        nameof(TimeText), typeof(string), typeof(EventRow), string.Empty);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(EventRow), string.Empty);
    public static readonly BindableProperty LocationProperty = BindableProperty.Create(
        nameof(Location), typeof(string), typeof(EventRow), string.Empty);
    public static readonly BindableProperty ChildSummaryProperty = BindableProperty.Create(
        nameof(ChildSummary), typeof(string), typeof(EventRow), string.Empty);
    public static readonly BindableProperty ChildAccentColorProperty = BindableProperty.Create(
        nameof(ChildAccentColor), typeof(Color), typeof(EventRow), Colors.Transparent);
    public static readonly BindableProperty IsUpdatedProperty = BindableProperty.Create(
        nameof(IsUpdated), typeof(bool), typeof(EventRow), false);

    public EventRow()
    {
        InitializeComponent();
    }

    public string TimeText { get => (string)GetValue(TimeTextProperty); set => SetValue(TimeTextProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Location { get => (string)GetValue(LocationProperty); set => SetValue(LocationProperty, value); }
    public string ChildSummary { get => (string)GetValue(ChildSummaryProperty); set => SetValue(ChildSummaryProperty, value); }
    public Color ChildAccentColor { get => (Color)GetValue(ChildAccentColorProperty); set => SetValue(ChildAccentColorProperty, value); }
    public bool IsUpdated { get => (bool)GetValue(IsUpdatedProperty); set => SetValue(IsUpdatedProperty, value); }
}
