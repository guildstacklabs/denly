using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Denly.ViewModels;

public class DayGroupViewModel
{
    public string DayHeader { get; set; } = string.Empty;
    public bool HasUpdates { get; set; }
    public ObservableCollection<EventRowViewModel> Events { get; } = new();
}

public class EventRowViewModel
{
    public string Id { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ChildSummary { get; set; } = string.Empty;
    public Color ChildAccentColor { get; set; } = Colors.Transparent;
    public bool IsUpdated { get; set; }
}

public class CalendarDayViewModel
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public int EventCount { get; set; }
    public List<Color> MarkerColors { get; set; } = new();
    public int OverflowCount { get; set; }
    public bool HasUpdates { get; set; }
}

public class WeekDayColumnViewModel
{
    public DateTime Date { get; set; }
    public string HeaderText { get; set; } = string.Empty;
    public ObservableCollection<WeekEventBlockViewModel> Events { get; } = new();
}

public class WeekEventBlockViewModel
{
    public string EventId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string ChildSummary { get; set; } = string.Empty;
    public int StartMinutes { get; set; }
    public int DurationMinutes { get; set; }
    public int Lane { get; set; }
    public Thickness LaneMargin { get; set; }
    public Color AccentColor { get; set; } = Colors.Transparent;
    public bool IsUpdated { get; set; }
}
