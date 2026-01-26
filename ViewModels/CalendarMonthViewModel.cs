using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Denly.Models;
using Denly.Services;
using Microsoft.Maui.Graphics;

namespace Denly.ViewModels;

public class CalendarMonthViewModel : INotifyPropertyChanged
{
    private readonly IScheduleService _scheduleService;
    private readonly IChildService _childService;
    private readonly ISeenStateService _seenStateService;
    private readonly IDenTimeService _denTimeService;
    private TimeZoneInfo? _denTimeZone;

    private List<Event> _allEvents = new();
    private List<Child> _children = new();
    private Dictionary<string, DateTime?> _seenMap = new();
    private int _year;
    private int _month;

    public CalendarMonthViewModel(IScheduleService scheduleService, IChildService childService, ISeenStateService seenStateService, IDenTimeService denTimeService)
    {
        _scheduleService = scheduleService;
        _childService = childService;
        _seenStateService = seenStateService;
        _denTimeService = denTimeService;
        ToggleChildFilterCommand = new Command<string?>(ToggleChildFilter);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CalendarDayViewModel> Days { get; } = new();
    public ObservableCollection<ChildFilterItem> ChildFilters { get; } = new();

    public ICommand ToggleChildFilterCommand { get; }

    public bool IsFilterActive
    {
        get => _isFilterActive;
        set => SetField(ref _isFilterActive, value);
    }

    public bool HasUpdates
    {
        get => _hasUpdates;
        set => SetField(ref _hasUpdates, value);
    }

    public async Task LoadAsync(int year, int month)
    {
        _year = year;
        _month = month;
        _denTimeZone = await _denTimeService.GetDenTimeZoneAsync();
        _allEvents = await _scheduleService.GetEventsByMonthAsync(year, month);
        _children = await _childService.GetActiveChildrenAsync();
        _seenMap = await _seenStateService.GetSeenMapAsync(_allEvents.Select(e => e.Id));

        ChildFilters.Clear();
        foreach (var child in _children)
        {
            ChildFilters.Add(new ChildFilterItem
            {
                ChildId = child.Id,
                Name = _childService.GetDisplayName(child, _children),
                AccentColor = GetChildColor(_children, child.Id)
            });
        }

        IsFilterActive = false;
        BuildMonth();
    }

    private Color GetChildColor(List<Child> children, string? childId)
    {
        var child = children.FirstOrDefault(c => c.Id == childId);
        if (child == null || string.IsNullOrWhiteSpace(child.Color)) return Colors.Transparent;
        try { return Color.FromArgb(child.Color); } catch { return Colors.Transparent; }
    }

    private void ToggleChildFilter(string? childId)
    {
        if (string.IsNullOrWhiteSpace(childId)) return;
        var item = ChildFilters.FirstOrDefault(c => c.ChildId == childId);
        if (item == null) return;
        item.IsSelected = !item.IsSelected;
        IsFilterActive = ChildFilters.Any(c => c.IsSelected);
        BuildMonth();
    }

    private void BuildMonth()
    {
        Days.Clear();
        HasUpdates = false;

        var activeIds = ChildFilters.Where(c => c.IsSelected).Select(c => c.ChildId).ToHashSet();
        var filtered = activeIds.Count == 0
            ? _allEvents
            : _allEvents.Where(e => !string.IsNullOrWhiteSpace(e.ChildId) && activeIds.Contains(e.ChildId!)).ToList();

        var first = new DateTime(_year, _month, 1);
        var start = first.AddDays(-(int)first.DayOfWeek);
        var today = _denTimeService.ConvertToDenTime(DateTime.Now, _denTimeZone).Date;

        for (int i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var dayEvents = filtered
                .Where(e => _denTimeService.ConvertToDenTime(e.StartsAt, _denTimeZone).Date == date.Date)
                .ToList();

            var markers = dayEvents
                .Take(3)
                .Select(e => GetChildColor(_children, e.ChildId))
                .ToList();

            var overflow = Math.Max(0, dayEvents.Count - 3);
            var hasUpdates = dayEvents.Any(e => !_seenMap.TryGetValue(e.Id, out var lastSeen) || e.UpdatedAt > (lastSeen ?? DateTime.MinValue));
            HasUpdates |= hasUpdates;

            Days.Add(new CalendarDayViewModel
            {
                Date = date,
                IsCurrentMonth = date.Month == _month,
                IsToday = date.Date == today,
                EventCount = dayEvents.Count,
                MarkerColors = markers,
                OverflowCount = overflow,
                HasUpdates = hasUpdates
            });
        }
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool _isFilterActive;
    private bool _hasUpdates;
}
