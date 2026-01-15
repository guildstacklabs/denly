using Denly.Models;
namespace Denly.Services;

public class SupabaseScheduleService : SupabaseServiceBase, IScheduleService
{
    public SupabaseScheduleService(IDenService denService, IAuthService authService)
        : base(denService, authService)
    {
    }

    public async Task<List<Event>> GetEventsByMonthAsync(int year, int month)
    {
        await EnsureInitializedAsync();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Event>();

        try
        {
            // Calculate the UTC range that covers the entire local month
            // We want everything from 00:00:00 on the 1st to 00:00:00 on the 1st of next month (Local Time)
            var localStart = new DateTime(year, month, 1);
            var localEnd = localStart.AddMonths(1);
            
            var utcStart = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
            var utcEnd = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();

            var response = await SupabaseClient!
                .From<Event>()
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, utcStart.ToString("O"))
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.LessThan, utcEnd.ToString("O"))
                .Order("starts_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var events = response.Models;
            foreach (var evt in events) ConvertToLocal(evt);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduleService] Error getting events by month");
            return new List<Event>();
        }
    }

    public async Task<List<Event>> GetEventsByDateAsync(DateTime date)
    {
        await EnsureInitializedAsync();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Event>();

        try
        {
            // Convert local date range to UTC for database query
            // Start of day in local time, converted to UTC
            var localStartOfDay = date.Date;
            var localEndOfDay = date.Date.AddDays(1).AddTicks(-1);
            var utcStart = localStartOfDay.ToUniversalTime();
            var utcEnd = localEndOfDay.ToUniversalTime();

            // Widen the upper bound of the DB query to ensure we catch events shifted by timezones.
            // We filter strictly in memory, so fetching a bit more data (e.g. next day's events) is safe.
            var utcEndBuffer = utcEnd.AddDays(1);

            _logger.LogInformation("[ScheduleService] GetEventsByDateAsync query prepared");

            // Optimization: Only fetch events that started recently enough to possibly overlap today.
            // Fetching ALL history (starts_at <= utcEnd) is inefficient.
            // Assuming max event duration is 30 days, we can filter starts_at >= (utcStart - 30 days).
            var lookbackBuffer = utcStart.AddDays(-30);

            var response = await SupabaseClient!
                .From<Event>()
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, lookbackBuffer.ToString("O"))
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, utcEndBuffer.ToString("O"))
                .Get();

            // Convert to Local Time immediately so filtering and UI display are correct
            foreach (var evt in response.Models) ConvertToLocal(evt);

            // Filter in memory with logging for diagnostics
            var filteredEvents = new List<Event>();
            foreach (var evt in response.Models)
            {
                // Check for single day match OR multi-day overlap
                // Note: EndsAt is already Local thanks to ConvertToLocal above
                var isMatch = evt.Date == date.Date || 
                             (evt.EndsAt.HasValue && evt.EndsAt.Value.Date >= date.Date && evt.Date <= date.Date);

                if (isMatch) filteredEvents.Add(evt);
            }

            filteredEvents = filteredEvents.OrderBy(e => e.StartsAt).ToList();

            _logger.LogInformation("[ScheduleService] GetEventsByDateAsync found {Count} events", filteredEvents.Count);

            return filteredEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduleService] Error getting events by date");
            return new List<Event>();
        }
    }

    public async Task<List<Event>> GetUpcomingEventsAsync(int count)
    {
        await EnsureInitializedAsync();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Event>();

        try
        {
            var now = _clock.UtcNow;

            var response = await SupabaseClient!
                .From<Event>()
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, now.ToString("yyyy-MM-ddTHH:mm:ss"))
                .Order("starts_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Limit(count)
                .Get();

            var events = response.Models;
            foreach (var evt in events) ConvertToLocal(evt);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduleService] Error getting upcoming events");
            return new List<Event>();
        }
    }

    public async Task<Event?> GetEventByIdAsync(string id)
    {
        await EnsureInitializedAsync();

        try
        {
            var evt = await SupabaseClient!
                .From<Event>()
                .Where(e => e.Id == id)
                .Single();
            
            if (evt != null) ConvertToLocal(evt);
            return evt;
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
        Console.WriteLine($"[ScheduleService] SaveEventAsync - Input StartsAt: {evt.StartsAt:O} (Kind: {evt.StartsAt.Kind})");

        await EnsureInitializedAsync();

        var denId = DenService.GetCurrentDenId();
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

        // Prepare UTC times for storage
        // Handle Kind correctly: Utc stays Utc, Local/Unspecified converts to Utc
        // This prevents double-conversion if the input is already UTC
        var startUtc = evt.StartsAt.Kind == DateTimeKind.Utc
            ? evt.StartsAt
            : DateTime.SpecifyKind(evt.StartsAt, DateTimeKind.Local).ToUniversalTime();

        var endUtc = evt.EndsAt.HasValue
            ? (evt.EndsAt.Value.Kind == DateTimeKind.Utc
                ? evt.EndsAt.Value
                : DateTime.SpecifyKind(evt.EndsAt.Value, DateTimeKind.Local).ToUniversalTime())
            : (DateTime?)null;

        if (existing != null)
        {
            _logger.LogInformation("[ScheduleService] Updating existing event");
            try
            {
                await SupabaseClient!
                    .From<Event>()
                    .Where(e => e.Id == evt.Id)
                    .Set(e => e.Title, evt.Title)
                    .Set(e => e.StartsAt, startUtc)
                    .Set(e => e.EndsAt!, endUtc)
                    .Set(e => e.AllDay, evt.AllDay)
                    .Set(e => e.EventTypeString, evt.EventTypeString)
                    .Set(e => e.Location!, evt.Location)
                    .Set(e => e.Notes!, evt.Notes)
                    .Set(e => e.ChildId!, evt.ChildId)
                    .Update();
                _logger.LogInformation("[ScheduleService] Event updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ScheduleService] Error updating event");
            }
        }
        else
        {
            _logger.LogInformation("[ScheduleService] Inserting new event");
            evt.CreatedBy = supabaseUser.Id;
            evt.CreatedAt = DateTime.UtcNow;

            // Temporarily set UTC times on the object for insertion
            var originalStart = evt.StartsAt;
            var originalEnd = evt.EndsAt;

            evt.StartsAt = startUtc;
            evt.EndsAt = endUtc;

            try
            {
                var response = await SupabaseClient!
                    .From<Event>()
                    .Insert(evt);
                _logger.LogInformation("[ScheduleService] Event insert response: {Count} models returned", response?.Models?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ScheduleService] Error inserting event");
            }
            finally
            {
                // Restore Local times so the UI object remains correct
                evt.StartsAt = originalStart;
                evt.EndsAt = originalEnd;
            }
        }
    }

    public async Task DeleteEventAsync(string id)
    {
        _logger.LogInformation("[ScheduleService] DeleteEventAsync called");
        await EnsureInitializedAsync();

        try
        {
            await SupabaseClient!
                .From<Event>()
                .Where(e => e.Id == id)
                .Delete();
            _logger.LogInformation("[ScheduleService] Event deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduleService] Error deleting event");
        }
    }

    private void ConvertToLocal(Event evt)
    {
        // Supabase (via Newtonsoft) often converts to Local time automatically but leaves Kind as Unspecified.
        // If we call ToLocalTime() on that, it subtracts the offset AGAIN (Double Conversion).
        // Fix: Only convert if the Kind is explicitly UTC.
        if (evt.StartsAt.Kind == DateTimeKind.Utc)
        {
            evt.StartsAt = evt.StartsAt.ToLocalTime();
        }

        if (evt.EndsAt.HasValue && evt.EndsAt.Value.Kind == DateTimeKind.Utc)
        {
            evt.EndsAt = evt.EndsAt.Value.ToLocalTime();
        }
    }
}
