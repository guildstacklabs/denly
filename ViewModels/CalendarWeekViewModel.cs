using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Denly.Models;
using Denly.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Denly.ViewModels;

public class CalendarWeekViewModel : INotifyPropertyChanged
{
    private readonly IScheduleService _scheduleService;
    private readonly IChildService _childService;
    private readonly ISeenStateService _seenStateService;
    private readonly IDenTimeService _denTimeService;
    private TimeZoneInfo? _denTimeZone;

    private DateTime _startOfWeek;
    private List<Event> _allEvents = new();
    private List<Child> _children = new();
    private Dictionary<string, DateTime?> _seenMap = new();

    public CalendarWeekViewModel(IScheduleService scheduleService, IChildService childService, ISeenStateService seenStateService, IDenTimeService denTimeService)
    {
        _scheduleService = scheduleService;
        _childService = childService;
        _seenStateService = seenStateService;
        _denTimeService = denTimeService;
        ToggleChildFilterCommand = new Command<string?>(ToggleChildFilter);
        SelectEventCommand = new Command<string?>(OnSelectEvent);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<WeekDayColumnViewModel> WeekDays { get; } = new();
    public ObservableCollection<ChildFilterItem> ChildFilters { get; } = new();

    public ICommand ToggleChildFilterCommand { get; }
    public ICommand SelectEventCommand { get; }

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

    public async Task LoadAsync(DateTime startOfWeek)
    {
        _startOfWeek = startOfWeek.Date;
        _denTimeZone = await _denTimeService.GetDenTimeZoneAsync();
        var end = _startOfWeek.AddDays(6);
        _allEvents = await _scheduleService.GetEventsByRangeAsync(_startOfWeek, end);
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
        BuildWeek();
    }

    private Color GetChildColor(List<Child> children, string? childId)
    {
        var child = children.FirstOrDefault(c => c.Id == childId);
        if (child == null || string.IsNullOrWhiteSpace(child.Color)) return Colors.Transparent;
        try { return Color.FromArgb(child.Color); } catch { return Colors.Transparent; }
    }

    private string GetChildSummary(List<Child> children, string? childId)
    {
        if (string.IsNullOrWhiteSpace(childId)) return string.Empty;
        var child = children.FirstOrDefault(c => c.Id == childId);
        return child == null ? string.Empty : _childService.GetDisplayName(child, children);
    }

    private void ToggleChildFilter(string? childId)
    {
        if (string.IsNullOrWhiteSpace(childId)) return;
        var item = ChildFilters.FirstOrDefault(c => c.ChildId == childId);
        if (item == null) return;
        item.IsSelected = !item.IsSelected;
        IsFilterActive = ChildFilters.Any(c => c.IsSelected);
        BuildWeek();
    }

    private void BuildWeek()
    {
        WeekDays.Clear();
        HasUpdates = false;

        var activeIds = ChildFilters.Where(c => c.IsSelected).Select(c => c.ChildId).ToHashSet();
        var filtered = activeIds.Count == 0
            ? _allEvents
            : _allEvents.Where(e => !string.IsNullOrWhiteSpace(e.ChildId) && activeIds.Contains(e.ChildId!)).ToList();

        for (int i = 0; i < 7; i++)
        {
            var date = _startOfWeek.AddDays(i);
            var column = new WeekDayColumnViewModel
            {
                Date = date,
                HeaderText = _denTimeService.ConvertToDenTime(date, _denTimeZone).ToString("ddd d")
            };

            var dayEvents = filtered
                .Where(e => _denTimeService.ConvertToDenTime(e.StartsAt, _denTimeZone).Date == date.Date)
                .OrderBy(e => e.StartsAt)
                .ToList();

            var lanes = new List<DateTime>();

            foreach (var evt in dayEvents)
            {
                var updated = !_seenMap.TryGetValue(evt.Id, out var lastSeen) || evt.UpdatedAt > (lastSeen ?? DateTime.MinValue);
                HasUpdates |= updated;

                var start = _denTimeService.ConvertToDenTime(evt.StartsAt, _denTimeZone);
                var endTime = evt.EndsAt.HasValue
                    ? _denTimeService.ConvertToDenTime(evt.EndsAt.Value, _denTimeZone)
                    : start.AddHours(1);

                var startMinutes = start.Hour * 60 + start.Minute;
                var duration = (int)(endTime - start).TotalMinutes;

                var lane = 0;
                while (lane < lanes.Count && lanes[lane] > start) lane++;
                if (lane == lanes.Count) lanes.Add(endTime);
                else lanes[lane] = endTime;

                column.Events.Add(new WeekEventBlockViewModel
                {
                    EventId = evt.Id,
                    Title = evt.Title,
                    TimeText = evt.AllDay ? "All day" : start.ToString("h:mm tt"),
                    ChildSummary = GetChildSummary(_children, evt.ChildId),
                    StartMinutes = startMinutes,
                    DurationMinutes = Math.Max(30, duration),
                    Lane = lane,
                    LaneMargin = new Thickness(lane * 6, 0, 0, 0),
                    AccentColor = GetChildColor(_children, evt.ChildId),
                    IsUpdated = updated
                });
            }

            WeekDays.Add(column);
        }
    }

    private async void OnSelectEvent(string? eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return;
        // Route should be registered when EventDetailsPage is added.
        await Shell.Current.GoToAsync($"event-details?id={eventId}");
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
