using Denly.Models;
using Microsoft.Extensions.Logging;

namespace Denly.Services;

public class SupabaseScheduleService : SupabaseServiceBase, IScheduleService
{
    private readonly ILogger<SupabaseScheduleService> _logger;

    public SupabaseScheduleService(IDenService denService, IAuthService authService, ILogger<SupabaseScheduleService> logger)
        : base(denService, authService)
    {
        _logger = logger;
    }

    public async Task<List<Event>> GetEventsByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Event>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Calculate the UTC range that covers the entire local month
            // We want everything from 00:00:00 on the 1st to 00:00:00 on the 1st of next month (Local Time)
            var localStart = new DateTime(year, month, 1);
            var localEnd = localStart.AddMonths(1);

            var utcStart = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
            var utcEnd = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();

            cancellationToken.ThrowIfCancellationRequested();

            var response = await GetClientOrThrow()
                .From<Event>()
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at")
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, utcStart.ToString("O"))
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.LessThan, utcEnd.ToString("O"))
                .Order("starts_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var events = response.Models;
            foreach (var evt in events) ConvertToLocal(evt);
            return events;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events by month");
            return new List<Event>();
        }
    }

    public async Task<List<Event>> GetEventsByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Event>();

        cancellationToken.ThrowIfCancellationRequested();

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

            // Optimization: Only fetch events that started recently enough to possibly overlap today.
            // Fetching ALL history (starts_at <= utcEnd) is inefficient.
            // Assuming max event duration is 30 days, we can filter starts_at >= (utcStart - 30 days).
            var lookbackBuffer = utcStart.AddDays(-30);

            cancellationToken.ThrowIfCancellationRequested();

            var response = await GetClientOrThrow()
                .From<Event>()
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at")
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

            _logger.LogDebug("Found {Count} events for date", filteredEvents.Count);

            return filteredEvents;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events by date");
            return new List<Event>();
        }
    }

    public async Task<List<Event>> GetUpcomingEventsAsync(int count, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Event>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var now = DateTime.UtcNow;

            cancellationToken.ThrowIfCancellationRequested();

            var response = await GetClientOrThrow()
                .From<Event>()
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at")
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, now.ToString("yyyy-MM-ddTHH:mm:ss"))
                .Order("starts_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Limit(count)
                .Get();

            var events = response.Models;
            foreach (var evt in events) ConvertToLocal(evt);
            return events;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get upcoming events");
            return new List<Event>();
        }
    }

    public async Task<Event?> GetEventByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await GetClientOrThrow()
                .From<Event>()
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at")
                .Where(e => e.Id == id)
                .Limit(1)
                .Get();

            var evt = response.Models.FirstOrDefault();
            if (evt != null)
            {
                ConvertToLocal(evt);
            }
            return evt;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event by ID");
            return null;
        }
    }

    public async Task SaveEventAsync(Event evt, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SaveEventAsync called");

        await EnsureInitializedAsync();

        var denId = GetCurrentDenIdOrThrow();
        var userId = GetAuthenticatedUserIdOrThrow();
        var client = GetClientOrThrow();

        cancellationToken.ThrowIfCancellationRequested();

        evt.DenId = denId;

        // Check if this is an update or insert
        var existing = await GetEventByIdAsync(evt.Id, cancellationToken);

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

        cancellationToken.ThrowIfCancellationRequested();

        if (existing != null)
        {
            _logger.LogDebug("Updating existing event");
            try
            {
                await client
                    .From<Event>()
                    .Where(e => e.Id == evt.Id)
                    .Set(e => e.Title, evt.Title)
                    .Set(e => e.StartsAt, startUtc)
                    .Set(e => e.EndsAtRaw!, endUtc)
                    .Set(e => e.AllDay, evt.AllDay)
                    .Set(e => e.EventTypeString, evt.EventTypeString)
                    .Set(e => e.Location!, evt.Location)
                    .Set(e => e.Notes!, evt.Notes)
                    .Set(e => e.ChildId!, evt.ChildId)
                    .Update();
                _logger.LogDebug("Event updated successfully");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update event");
            }
        }
        else
        {
            _logger.LogDebug("Inserting new event");
            evt.CreatedBy = userId;
            evt.CreatedAt = DateTime.UtcNow;

            // Temporarily set UTC times on the object for insertion
            var originalStart = evt.StartsAt;
            var originalEnd = evt.EndsAt;

            evt.StartsAt = startUtc;
            evt.EndsAt = endUtc;

            try
            {
                await client
                    .From<Event>()
                    .Insert(evt);
                _logger.LogDebug("Event inserted successfully");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert event");
            }
            finally
            {
                // Restore Local times so the UI object remains correct
                evt.StartsAt = originalStart;
                evt.EndsAt = originalEnd;
            }
        }
    }

    public async Task DeleteEventAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("DeleteEventAsync called");
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await GetClientOrThrow()
                .From<Event>()
                .Where(e => e.Id == id)
                .Delete();
            _logger.LogDebug("Event deleted successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete event");
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

    public async Task<bool> HasUpcomingEventsAsync()
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return false;

        try
        {
            var now = DateTime.UtcNow;
            var result = await GetClientOrThrow()
                .From<Event>()
                .Select("id")
                .Filter("den_id", Supabase.Postgrest.Constants.Operator.Equals, denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, now.ToString("o"))
                .Limit(1)
                .Get();

            return result.Models.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for upcoming events");
            return false;
        }
    }
}
