using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Denly.Models;
using Denly.Services;
using Microsoft.Maui.Graphics;

namespace Denly.ViewModels;

public class CalendarAgendaViewModel : INotifyPropertyChanged
{
    private readonly IScheduleService _scheduleService;
    private readonly IChildService _childService;
    private readonly ISeenStateService _seenStateService;
    private readonly IDenTimeService _denTimeService;
    private TimeZoneInfo? _denTimeZone;

    public CalendarAgendaViewModel(IScheduleService scheduleService, IChildService childService, ISeenStateService seenStateService, IDenTimeService denTimeService)
    {
        _scheduleService = scheduleService;
        _childService = childService;
        _seenStateService = seenStateService;
        _denTimeService = denTimeService;
        ToggleChildFilterCommand = new Command<string?>(ToggleChildFilter);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DayGroupViewModel> DayGroups { get; } = new();
    public ObservableCollection<ChildFilterItem> ChildFilters { get; } = new();
    public ICommand ToggleChildFilterCommand { get; }

    public bool HasUpdates
    {
        get => _hasUpdates;
        set => SetField(ref _hasUpdates, value);
    }

    public bool IsFilterActive
    {
        get => _isFilterActive;
        set => SetField(ref _isFilterActive, value);
    }

    public string EmptyMessage
    {
        get => _emptyMessage;
        set => SetField(ref _emptyMessage, value);
    }

    public async Task LoadAsync(DateTime? focusDate = null)
    {
        _denTimeZone = await _denTimeService.GetDenTimeZoneAsync();
        var start = focusDate?.Date ?? _denTimeService.ConvertToDenTime(DateTime.Now, _denTimeZone).Date;
        var end = start.AddDays(13);

        _allEvents = await _scheduleService.GetEventsByRangeAsync(start, end);
        _children = await _childService.GetActiveChildrenAsync();
        _seenMap = await _seenStateService.GetSeenMapAsync(_allEvents.Select(e => e.Id));

        ChildFilters.Clear();
        foreach (var child in _children)
        {
            ChildFilters.Add(new ChildFilterItem
            {
                ChildId = child.Id,
                Name = _childService.GetDisplayName(child, _children),
                AccentColor = ParseColor(child.Color)
            });
        }

        IsFilterActive = false;
        BuildGroups();
    }

    private string GetChildSummary(List<Child> children, string? childId)
    {
        if (string.IsNullOrWhiteSpace(childId)) return string.Empty;
        var child = children.FirstOrDefault(c => c.Id == childId);
        return child == null ? string.Empty : _childService.GetDisplayName(child, children);
    }

    private Color GetChildColor(List<Child> children, string? childId)
    {
        var child = children.FirstOrDefault(c => c.Id == childId);
        return ParseColor(child?.Color);
    }

    private static Color ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Colors.Transparent;
        try { return Color.FromArgb(hex); } catch { return Colors.Transparent; }
    }

    private bool _hasUpdates;
    private bool _isFilterActive;
    private string _emptyMessage = string.Empty;

    private List<Event> _allEvents = new();
    private List<Child> _children = new();
    private Dictionary<string, DateTime?> _seenMap = new();

    private void ToggleChildFilter(string? childId)
    {
        if (string.IsNullOrWhiteSpace(childId)) return;
        var item = ChildFilters.FirstOrDefault(c => c.ChildId == childId);
        if (item == null) return;
        item.IsSelected = !item.IsSelected;
        IsFilterActive = ChildFilters.Any(c => c.IsSelected);
        BuildGroups();
    }

    private void BuildGroups()
    {
        DayGroups.Clear();
        HasUpdates = false;

        var activeIds = ChildFilters.Where(c => c.IsSelected).Select(c => c.ChildId).ToHashSet();
        var filtered = activeIds.Count == 0
            ? _allEvents
            : _allEvents.Where(e => !string.IsNullOrWhiteSpace(e.ChildId) && activeIds.Contains(e.ChildId!)).ToList();

        var grouped = filtered
            .OrderBy(e => e.StartsAt)
            .GroupBy(e => _denTimeService.ConvertToDenTime(e.StartsAt, _denTimeZone).Date);

        foreach (var group in grouped)
        {
            var dayGroup = new DayGroupViewModel
            {
                DayHeader = _denTimeService.FormatDayHeader(group.Key, _denTimeZone),
            };

            foreach (var evt in group)
            {
                var updated = !_seenMap.TryGetValue(evt.Id, out var lastSeen) || evt.UpdatedAt > (lastSeen ?? DateTime.MinValue);
                HasUpdates |= updated;
                var startsAt = _denTimeService.ConvertToDenTime(evt.StartsAt, _denTimeZone);
                dayGroup.Events.Add(new EventRowViewModel
                {
                    Id = evt.Id,
                    TimeText = evt.AllDay ? "All day" : startsAt.ToString("h:mm tt"),
                    Title = evt.Title,
                    Location = evt.Location ?? string.Empty,
                    ChildSummary = GetChildSummary(_children, evt.ChildId),
                    ChildAccentColor = GetChildColor(_children, evt.ChildId),
                    IsUpdated = updated
                });
            }

            dayGroup.HasUpdates = dayGroup.Events.Any(e => e.IsUpdated);
            DayGroups.Add(dayGroup);
        }

        EmptyMessage = DayGroups.Count == 0 ? "No events in the next two weeks." : string.Empty;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
