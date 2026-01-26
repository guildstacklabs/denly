using Denly.Services;
using Denly.ViewModels;

namespace Denly.Pages;

public partial class CalendarWeekPage : ContentPage
{
    private readonly CalendarWeekViewModel _viewModel;

    public CalendarWeekPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var schedule = services?.GetService(typeof(IScheduleService)) as IScheduleService;
        var child = services?.GetService(typeof(IChildService)) as IChildService;
        var seen = services?.GetService(typeof(ISeenStateService)) as ISeenStateService;
        var denTime = services?.GetService(typeof(IDenTimeService)) as IDenTimeService;

        _viewModel = new CalendarWeekViewModel(
            schedule ?? new StubScheduleService(),
            child ?? new StubChildService(),
            seen ?? new StubSeenStateService(),
            denTime ?? new DenTimeService(new CalendarFallbackDenService()));

        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var start = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        await _viewModel.LoadAsync(start);
    }
}
