using Denly.Models;
using Supabase;

namespace Denly.Services;

public class SupabaseScheduleService : IScheduleService
{
    private readonly IDenService _denService;
    private readonly IAuthService _authService;
    private bool _isInitialized;

    // Use the authenticated client from AuthService
    private Supabase.Client? SupabaseClient => _authService.GetSupabaseClient();

    public SupabaseScheduleService(IDenService denService, IAuthService authService)
    {
        _denService = denService;
        _authService = authService;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        // Ensure auth service is initialized (which creates the authenticated client)
        await _authService.InitializeAsync();
        // Ensure den service is initialized (to restore current den from storage)
        await _denService.InitializeAsync();
        _isInitialized = true;
    }

    public async Task<List<Event>> GetEventsByMonthAsync(int year, int month)
    {
        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Event>();

        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var response = await SupabaseClient!
                .From<Event>()
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, startDate.ToString("yyyy-MM-dd"))
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, endDate.ToString("yyyy-MM-dd"))
                .Order("starts_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScheduleService] Error getting events by month: {ex.Message}");
            return new List<Event>();
        }
    }

    public async Task<List<Event>> GetEventsByDateAsync(DateTime date)
    {
        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Event>();

        try
        {
            // Convert local date range to UTC for database query
            // Start of day in local time, converted to UTC
            var localStartOfDay = date.Date;
            var localEndOfDay = date.Date.AddDays(1).AddTicks(-1);
            var utcStart = localStartOfDay.ToUniversalTime();
            var utcEnd = localEndOfDay.ToUniversalTime();

            Console.WriteLine($"[ScheduleService] GetEventsByDateAsync - local date: {date:yyyy-MM-dd}, UTC range: {utcStart:O} to {utcEnd:O}");

            var response = await SupabaseClient!
                .From<Event>()
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, utcEnd.ToString("O"))
                .Get();

            // Filter in memory for events that fall on this local date
            // Use the Event.Date property which already converts to local time
            var filteredEvents = response.Models
                .Where(e => e.Date == date.Date ||
                           (e.EndsAt != null && e.EndsAt.Value.ToLocalTime().Date >= date.Date && e.Date <= date.Date))
                .OrderBy(e => e.StartsAt)
                .ToList();

            Console.WriteLine($"[ScheduleService] GetEventsByDateAsync - found {filteredEvents.Count} events for {date:yyyy-MM-dd}");
            foreach (var evt in filteredEvents)
            {
                Console.WriteLine($"[ScheduleService]   - {evt.Title}: StartsAt(UTC)={evt.StartsAt:O}, Date(Local)={evt.Date:yyyy-MM-dd}");
            }

            return filteredEvents;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScheduleService] Error getting events by date: {ex.Message}");
            return new List<Event>();
        }
    }

    public async Task<List<Event>> GetUpcomingEventsAsync(int count)
    {
        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Event>();

        try
        {
            var now = DateTime.UtcNow;

            var response = await SupabaseClient!
                .From<Event>()
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, now.ToString("yyyy-MM-ddTHH:mm:ss"))
                .Order("starts_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Limit(count)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScheduleService] Error getting upcoming events: {ex.Message}");
            return new List<Event>();
        }
    }

    public async Task<Event?> GetEventByIdAsync(string id)
    {
        await EnsureInitializedAsync();

        try
        {
            return await SupabaseClient!
                .From<Event>()
                .Where(e => e.Id == id)
                .Single();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScheduleService] Error getting event by id: {ex.Message}");
            return null;
        }
    }

    public async Task SaveEventAsync(Event evt)
    {
        Console.WriteLine($"[ScheduleService] SaveEventAsync called for: {evt.Title}");

        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            Console.WriteLine("[ScheduleService] Error: No den selected");
            return;
        }
        Console.WriteLine($"[ScheduleService] Den ID: {denId}");

        // Get user ID directly from the Supabase auth session
        var supabaseUser = SupabaseClient?.Auth.CurrentUser;
        if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
        {
            Console.WriteLine("[ScheduleService] Error: No authenticated Supabase session");
            return;
        }

        var userId = supabaseUser.Id;
        Console.WriteLine($"[ScheduleService] Supabase auth.uid(): {userId}");
        Console.WriteLine($"[ScheduleService] Session access token present: {!string.IsNullOrEmpty(SupabaseClient?.Auth.CurrentSession?.AccessToken)}");

        evt.DenId = denId;

        // Check if this is an update or insert
        var existing = await GetEventByIdAsync(evt.Id);

        if (existing != null)
        {
            Console.WriteLine($"[ScheduleService] Updating existing event: {evt.Id}");
            try
            {
                await SupabaseClient!
                    .From<Event>()
                    .Where(e => e.Id == evt.Id)
                    .Set(e => e.Title, evt.Title)
                    .Set(e => e.StartsAt, evt.StartsAt)
                    .Set(e => e.EndsAt!, evt.EndsAt)
                    .Set(e => e.AllDay, evt.AllDay)
                    .Set(e => e.EventTypeString, evt.EventTypeString)
                    .Set(e => e.Location!, evt.Location)
                    .Set(e => e.Notes!, evt.Notes)
                    .Set(e => e.ChildId!, evt.ChildId)
                    .Update();
                Console.WriteLine($"[ScheduleService] Event updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScheduleService] Error updating event: {ex.Message}");
                Console.WriteLine($"[ScheduleService] Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            Console.WriteLine($"[ScheduleService] Inserting new event: {evt.Id}");
            evt.CreatedBy = userId;
            evt.CreatedAt = DateTime.UtcNow;

            Console.WriteLine($"[ScheduleService] Event object - Id: {evt.Id}, DenId: {evt.DenId}, CreatedBy: {evt.CreatedBy}, Title: {evt.Title}");

            try
            {
                var response = await SupabaseClient!
                    .From<Event>()
                    .Insert(evt);
                Console.WriteLine($"[ScheduleService] Event insert response: {response?.Models?.Count ?? 0} models returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScheduleService] Error inserting event: {ex.Message}");
                Console.WriteLine($"[ScheduleService] Stack trace: {ex.StackTrace}");
            }
        }
    }

    public async Task DeleteEventAsync(string id)
    {
        Console.WriteLine($"[ScheduleService] DeleteEventAsync called for: {id}");
        await EnsureInitializedAsync();

        try
        {
            await SupabaseClient!
                .From<Event>()
                .Where(e => e.Id == id)
                .Delete();
            Console.WriteLine($"[ScheduleService] Event deleted successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScheduleService] Error deleting event: {ex.Message}");
            Console.WriteLine($"[ScheduleService] Stack trace: {ex.StackTrace}");
        }
    }
}
