using Denly.Models;

namespace Denly.Services;

public interface IScheduleService
{
    Task<List<Event>> GetEventsByMonthAsync(int year, int month);
    Task<List<Event>> GetEventsByDateAsync(DateTime date);
    Task<List<Event>> GetUpcomingEventsAsync(int count);
    Task<Event?> GetEventByIdAsync(string id);
    Task SaveEventAsync(Event evt);
    Task DeleteEventAsync(string id);
}
