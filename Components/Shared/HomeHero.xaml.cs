using Microsoft.Maui.Controls;

namespace Denly.Components.Shared;

public partial class HomeHero : ContentView
{
    public static readonly BindableProperty WelcomeTextProperty = BindableProperty.Create(
        nameof(WelcomeText),
        typeof(string),
        typeof(HomeHero),
        string.Empty);

    public static readonly BindableProperty DateRangeTextProperty = BindableProperty.Create(
        nameof(DateRangeText),
        typeof(string),
        typeof(HomeHero),
        string.Empty);

    public static readonly BindableProperty NextEventSummaryProperty = BindableProperty.Create(
        nameof(NextEventSummary),
        typeof(string),
        typeof(HomeHero),
        string.Empty);

    public static readonly BindableProperty HasUpdatesProperty = BindableProperty.Create(
        nameof(HasUpdates),
        typeof(bool),
        typeof(HomeHero),
        false);

    public HomeHero()
    {
        InitializeComponent();
    }

    public string WelcomeText
    {
        get => (string)GetValue(WelcomeTextProperty);
        set => SetValue(WelcomeTextProperty, value);
    }

    public string DateRangeText
    {
        get => (string)GetValue(DateRangeTextProperty);
        set => SetValue(DateRangeTextProperty, value);
    }

    public string NextEventSummary
    {
        get => (string)GetValue(NextEventSummaryProperty);
        set => SetValue(NextEventSummaryProperty, value);
    }

    public bool HasUpdates
    {
        get => (bool)GetValue(HasUpdatesProperty);
        set => SetValue(HasUpdatesProperty, value);
    }
}
