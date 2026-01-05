using System.Text.Json;
using Denly.Models;

namespace Denly.Services;

public interface IScheduleService
{
    Task<List<ScheduleEvent>> GetAllEventsAsync();
    Task<List<ScheduleEvent>> GetEventsByDateAsync(DateTime date);
    Task<List<ScheduleEvent>> GetEventsByMonthAsync(int year, int month);
    Task<ScheduleEvent?> GetEventByIdAsync(string id);
    Task SaveEventAsync(ScheduleEvent evt);
    Task DeleteEventAsync(string id);
}

public class LocalScheduleService : IScheduleService
{
    private readonly string _filePath;
    private List<ScheduleEvent>? _cache;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LocalScheduleService()
    {
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "schedule_events.json");
    }

    private async Task<List<ScheduleEvent>> LoadEventsAsync()
    {
        if (_cache != null)
            return _cache;

        if (!File.Exists(_filePath))
        {
            _cache = new List<ScheduleEvent>();
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _cache = JsonSerializer.Deserialize<List<ScheduleEvent>>(json, _jsonOptions) ?? new List<ScheduleEvent>();
        }
        catch
        {
            _cache = new List<ScheduleEvent>();
        }

        return _cache;
    }

    private async Task SaveEventsAsync(List<ScheduleEvent> events)
    {
        var json = JsonSerializer.Serialize(events, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
        _cache = events;
    }

    public async Task<List<ScheduleEvent>> GetAllEventsAsync()
    {
        return await LoadEventsAsync();
    }

    public async Task<List<ScheduleEvent>> GetEventsByDateAsync(DateTime date)
    {
        var events = await LoadEventsAsync();
        return events.Where(e => e.Date.Date == date.Date)
                     .OrderBy(e => e.Time ?? TimeSpan.MaxValue)
                     .ToList();
    }

    public async Task<List<ScheduleEvent>> GetEventsByMonthAsync(int year, int month)
    {
        var events = await LoadEventsAsync();
        return events.Where(e => e.Date.Year == year && e.Date.Month == month)
                     .OrderBy(e => e.Date)
                     .ThenBy(e => e.Time ?? TimeSpan.MaxValue)
                     .ToList();
    }

    public async Task<ScheduleEvent?> GetEventByIdAsync(string id)
    {
        var events = await LoadEventsAsync();
        return events.FirstOrDefault(e => e.Id == id);
    }

    public async Task SaveEventAsync(ScheduleEvent evt)
    {
        var events = await LoadEventsAsync();
        var existing = events.FirstOrDefault(e => e.Id == evt.Id);

        if (existing != null)
        {
            events.Remove(existing);
            evt.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            evt.CreatedAt = DateTime.UtcNow;
            evt.UpdatedAt = DateTime.UtcNow;
        }

        events.Add(evt);
        await SaveEventsAsync(events);
    }

    public async Task DeleteEventAsync(string id)
    {
        var events = await LoadEventsAsync();
        var evt = events.FirstOrDefault(e => e.Id == id);

        if (evt != null)
        {
            events.Remove(evt);
            await SaveEventsAsync(events);
        }
    }
}
