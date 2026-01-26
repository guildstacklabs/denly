using Denly.Models;

namespace Denly.Services;

public interface IScheduleService
{
    Task<List<Event>> GetEventsByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<List<Event>> GetEventsByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<List<Event>> GetEventsByRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<Event>> GetUpcomingEventsAsync(int count, CancellationToken cancellationToken = default);
    Task<Event?> GetEventByIdAsync(string id, CancellationToken cancellationToken = default);
    Task SaveEventAsync(Event evt, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> HasUpcomingEventsAsync();
}
