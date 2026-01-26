using Microsoft.Maui.Controls;
using System.Collections;

namespace Denly.Components.Shared;

public partial class DayGroup : ContentView
{
    public static readonly BindableProperty DayHeaderProperty = BindableProperty.Create(
        nameof(DayHeader), typeof(string), typeof(DayGroup), string.Empty);
    public static readonly BindableProperty HasUpdatesProperty = BindableProperty.Create(
        nameof(HasUpdates), typeof(bool), typeof(DayGroup), false);
    public static readonly BindableProperty EventsProperty = BindableProperty.Create(
        nameof(Events), typeof(IEnumerable), typeof(DayGroup), null);

    public DayGroup()
    {
        InitializeComponent();
    }

    public string DayHeader
    {
        get => (string)GetValue(DayHeaderProperty);
        set => SetValue(DayHeaderProperty, value);
    }

    public bool HasUpdates
    {
        get => (bool)GetValue(HasUpdatesProperty);
        set => SetValue(HasUpdatesProperty, value);
    }

    public IEnumerable? Events
    {
        get => (IEnumerable?)GetValue(EventsProperty);
        set => SetValue(EventsProperty, value);
    }
}
