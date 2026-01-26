using System.Linq;
using Denly.Services;
using Denly.ViewModels;

namespace Denly.Pages;

public partial class CalendarMonthPage : ContentPage
{
    private readonly CalendarMonthViewModel _viewModel;

    public CalendarMonthPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var schedule = services?.GetService(typeof(IScheduleService)) as IScheduleService;
        var child = services?.GetService(typeof(IChildService)) as IChildService;
        var seen = services?.GetService(typeof(ISeenStateService)) as ISeenStateService;
        var denTime = services?.GetService(typeof(IDenTimeService)) as IDenTimeService;

        _viewModel = new CalendarMonthViewModel(
            schedule ?? new StubScheduleService(),
            child ?? new StubChildService(),
            seen ?? new StubSeenStateService(),
            denTime ?? new DenTimeService(new CalendarFallbackDenService()));

        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var today = DateTime.Today;
        await _viewModel.LoadAsync(today.Year, today.Month);
    }

    private async void OnDaySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Denly.ViewModels.CalendarDayViewModel day)
            return;

        var dateString = day.Date.ToString("yyyy-MM-dd");
        await Shell.Current.GoToAsync($"//calendar/agenda?date={dateString}");
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }
}
