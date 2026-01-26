using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Denly.Services;

namespace Denly.ViewModels;

public class HomeSnapshotViewModel : INotifyPropertyChanged
{
    private readonly IDenTimeService _denTimeService;

    public HomeSnapshotViewModel(IDenTimeService denTimeService)
    {
        _denTimeService = denTimeService;
        ToggleFilterCommand = new Command(() => IsFilterExpanded = !IsFilterExpanded);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WelcomeText
    {
        get => _welcomeText;
        set => SetField(ref _welcomeText, value);
    }

    public string DateRangeText
    {
        get => _dateRangeText;
        set => SetField(ref _dateRangeText, value);
    }

    public string NextEventSummary
    {
        get => _nextEventSummary;
        set => SetField(ref _nextEventSummary, value);
    }

    public bool HasUpdates
    {
        get => _hasUpdates;
        set => SetField(ref _hasUpdates, value);
    }

    public bool IsFilterExpanded
    {
        get => _isFilterExpanded;
        set => SetField(ref _isFilterExpanded, value);
    }

    public bool IsFilterActive
    {
        get => _isFilterActive;
        set => SetField(ref _isFilterActive, value);
    }

    public string FilterSummary
    {
        get => _filterSummary;
        set => SetField(ref _filterSummary, value);
    }

    public ObservableCollection<DayGroupViewModel> DayGroups { get; } = new();
    public ICommand ToggleFilterCommand { get; }

    public async Task LoadAsync()
    {
        // Placeholder demo data; replace with service data wiring.
        var tz = await _denTimeService.GetDenTimeZoneAsync();
        var today = _denTimeService.ConvertToDenTime(DateTime.UtcNow, tz).Date;

        WelcomeText = "Welcome home";
        DateRangeText = $"{today:MMM d} – {today.AddDays(3):MMM d}";
        NextEventSummary = "No events yet";
        HasUpdates = false;
        IsFilterExpanded = false;
        IsFilterActive = false;
        FilterSummary = "All children";

        DayGroups.Clear();
        for (var i = 0; i < 4; i++)
        {
            var date = today.AddDays(i);
            var group = new DayGroupViewModel
            {
                DayHeader = date.ToString("dddd, MMM d"),
                HasUpdates = false
            };

            // Max 5 events per day
            for (var j = 0; j < 2; j++)
            {
                group.Events.Add(new EventRowViewModel
                {
                    TimeText = "All day",
                    Title = "No events scheduled",
                    Location = string.Empty,
                    ChildSummary = string.Empty,
                    IsUpdated = false
                });
            }

            DayGroups.Add(group);
        }
    }

    private string _welcomeText = string.Empty;
    private string _dateRangeText = string.Empty;
    private string _nextEventSummary = string.Empty;
    private bool _hasUpdates;
    private bool _isFilterExpanded;
    private bool _isFilterActive;
    private string _filterSummary = string.Empty;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
