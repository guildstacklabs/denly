using Denly.Services;
using Denly.ViewModels;

namespace Denly.Pages;

[QueryProperty(nameof(FocusDate), "date")]
public partial class CalendarAgendaPage : ContentPage
{
    private readonly CalendarAgendaViewModel _viewModel;
    private DateTime? _focusDate;

    public CalendarAgendaPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var schedule = services?.GetService(typeof(IScheduleService)) as IScheduleService;
        var child = services?.GetService(typeof(IChildService)) as IChildService;
        var seen = services?.GetService(typeof(ISeenStateService)) as ISeenStateService;
        var denTime = services?.GetService(typeof(IDenTimeService)) as IDenTimeService;

        _viewModel = new CalendarAgendaViewModel(
            schedule ?? new StubScheduleService(),
            child ?? new StubChildService(),
            seen ?? new StubSeenStateService(),
            denTime ?? new DenTimeService(new CalendarFallbackDenService()));

        BindingContext = _viewModel;
    }

    public string? FocusDate
    {
        get => _focusDate?.ToString("yyyy-MM-dd");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _focusDate = null;
                return;
            }
            if (DateTime.TryParse(value, out var parsed))
            {
                _focusDate = parsed.Date;
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_focusDate);
    }
}
