using System.Security.Cryptography;
using Denly.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Denly.Services;

public class SupabaseScheduleService : SupabaseServiceBase, IScheduleService
{
    private readonly ILogger<SupabaseScheduleService> _logger;
    private readonly IDenTimeService _denTimeService;
    private readonly DenlyOptions _options;

    public SupabaseScheduleService(IDenService denService, IAuthService authService, IDenTimeService denTimeService, IOptions<DenlyOptions> options, ILogger<SupabaseScheduleService> logger)
        : base(denService, authService)
    {
        _logger = logger;
        _denTimeService = denTimeService;
        _options = options.Value;
    }

    public async Task<List<Event>> GetEventsByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Event>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var denTimeZone = await _denTimeService.GetDenTimeZoneAsync(cancellationToken);
            // Calculate the UTC range that covers the entire local month
            // We want everything from 00:00:00 on the 1st to 00:00:00 on the 1st of next month (Local Time)
            var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var localEnd = localStart.AddMonths(1);

            var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, denTimeZone);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, denTimeZone);

            cancellationToken.ThrowIfCancellationRequested();

            var response = await GetClientOrThrow()
                .From<Event>()
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at, updated_at")
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, utcStart.ToString("O"))
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.LessThan, utcEnd.ToString("O"))
                .Order("starts_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var events = response.Models;
            foreach (var evt in events) NormalizeToDenTime(evt, denTimeZone);
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
            var denTimeZone = await _denTimeService.GetDenTimeZoneAsync(cancellationToken);
            // Convert local date range to UTC for database query
            // Start of day in local time, converted to UTC
            var localStartOfDay = date.Date;
            var localEndOfDay = date.Date.AddDays(1).AddTicks(-1);
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localStartOfDay, DateTimeKind.Unspecified), denTimeZone);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localEndOfDay, DateTimeKind.Unspecified), denTimeZone);

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
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at, updated_at")
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, lookbackBuffer.ToString("O"))
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, utcEndBuffer.ToString("O"))
                .Get();

            // Convert to Local Time immediately so filtering and UI display are correct
            foreach (var evt in response.Models) NormalizeToDenTime(evt, denTimeZone);

            // Filter in memory with logging for diagnostics
            var filteredEvents = new List<Event>();
            foreach (var evt in response.Models)
            {
                var startDate = evt.StartsAt.Date;
                var endDate = evt.EndsAt.HasValue
                    ? evt.EndsAt.Value.Date
                    : startDate;

                // Check for single day match OR multi-day overlap
                // Note: EndsAt is already Local thanks to ConvertToLocal above
                var isMatch = startDate == date.Date ||
                             (endDate >= date.Date && startDate <= date.Date);

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
            var denTimeZone = await _denTimeService.GetDenTimeZoneAsync(cancellationToken);
            var now = DateTime.UtcNow;

            cancellationToken.ThrowIfCancellationRequested();

            var response = await GetClientOrThrow()
                .From<Event>()
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at, updated_at")
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, now.ToString("O"))
                .Order("starts_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Limit(count)
                .Get();

            var events = response.Models;
            foreach (var evt in events) NormalizeToDenTime(evt, denTimeZone);
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
            var denTimeZone = await _denTimeService.GetDenTimeZoneAsync(cancellationToken);
            var response = await GetClientOrThrow()
                .From<Event>()
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at, updated_at")
                .Where(e => e.Id == id)
                .Limit(1)
                .Get();

            var evt = response.Models.FirstOrDefault();
            if (evt != null)
            {
                NormalizeToDenTime(evt, denTimeZone);
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
        var denTimeZone = await _denTimeService.GetDenTimeZoneAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        evt.DenId = denId;

        // Check if this is an update or insert
        var existing = await GetEventByIdAsync(evt.Id, cancellationToken);

        // Prepare UTC times for storage
        // Handle Kind correctly: Utc stays Utc, Local/Unspecified converts to Utc
        // This prevents double-conversion if the input is already UTC
        var startLocal = DateTime.SpecifyKind(evt.StartsAt, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, denTimeZone);

        var endUtc = evt.EndsAt.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(evt.EndsAt.Value, DateTimeKind.Unspecified), denTimeZone)
            : (DateTime?)null;
        var endUtcToken = endUtc.HasValue ? new JValue(endUtc.Value) : JValue.CreateNull();

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
                    .Set(e => e.EndsAtRaw!, endUtcToken)
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

    private void NormalizeToDenTime(Event evt, TimeZoneInfo timeZone)
    {
        // Supabase (via Newtonsoft) can return Local time with Kind=Unspecified.
        // Normalize to Local without double-conversion.
        evt.StartsAt = NormalizeDateTime(evt.StartsAt, timeZone);

        if (evt.EndsAt.HasValue)
        {
            evt.EndsAt = NormalizeDateTime(evt.EndsAt.Value, timeZone);
        }
    }

    public async Task<List<Event>> GetEventsByRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<Event>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var denTimeZone = await _denTimeService.GetDenTimeZoneAsync(cancellationToken);
            var localStart = startDate.Date;
            var localEnd = endDate.Date.AddDays(1).AddTicks(-1);

            var utcStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified), denTimeZone);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified), denTimeZone);
            var lookbackStart = utcStart.AddDays(-30);

            cancellationToken.ThrowIfCancellationRequested();

            var response = await GetClientOrThrow()
                .From<Event>()
                .Select("id, den_id, child_id, title, event_type, starts_at, ends_at, all_day, location, notes, created_by, created_at, updated_at")
                .Where(e => e.DenId == denId)
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, lookbackStart.ToString("O"))
                .Filter("starts_at", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, utcEnd.ToString("O"))
                .Get();

            var events = response.Models;
            foreach (var evt in events) NormalizeToDenTime(evt, denTimeZone);

            var filtered = events
                .Where(e =>
                {
                    var start = e.StartsAt.Date;
                    var end = e.EndsAt?.Date ?? start;
                    return start <= localEnd.Date && end >= localStart.Date;
                })
                .OrderBy(e => e.StartsAt)
                .ToList();

            return filtered;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events by range");
            return new List<Event>();
        }
    }

    private static DateTime NormalizeDateTime(DateTime value, TimeZoneInfo timeZone)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, timeZone);
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

    public async Task<string> GetOrCreateSubscriptionUrlAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = GetCurrentDenIdOrThrow();
        var userId = GetAuthenticatedUserIdOrThrow();
        var client = GetClientOrThrow();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Check for existing subscription
            var existing = await client
                .From<CalendarSubscription>()
                .Select("*")
                .Where(s => s.DenId == denId && s.UserId == userId)
                .Limit(1)
                .Get();

            string token;
            if (existing.Models.Count > 0)
            {
                token = existing.Models[0].Token;
            }
            else
            {
                // Generate a secure random token
                token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

                var subscription = new CalendarSubscription
                {
                    DenId = denId,
                    UserId = userId,
                    Token = token
                };

                await client
                    .From<CalendarSubscription>()
                    .Insert(subscription);
            }

            // Build the webcal URL
            var baseUrl = _options.SupabaseUrl.TrimEnd('/');
            return $"{baseUrl}/functions/v1/calendar-ics?token={token}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create calendar subscription");
            throw;
        }
    }

    public async Task<List<EventChild>> GetEventChildrenAsync(IEnumerable<string> eventIds, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new List<EventChild>();

        var ids = eventIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0) return new List<EventChild>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await GetClientOrThrow()
                .From<EventChild>()
                .Select("id, event_id, child_id, den_id, created_at")
                .Where(ec => ec.DenId == denId)
                .Filter("event_id", Supabase.Postgrest.Constants.Operator.In, ids)
                .Get();

            return response.Models;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event children");
            return new List<EventChild>();
        }
    }

    public async Task SaveEventChildrenAsync(string eventId, List<string> childIds, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = GetCurrentDenIdOrThrow();
        var client = GetClientOrThrow();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Delete existing associations
            await client
                .From<EventChild>()
                .Where(ec => ec.EventId == eventId && ec.DenId == denId)
                .Delete();

            if (childIds.Count == 0) return;

            var associations = childIds.Select(childId => new EventChild
            {
                EventId = eventId,
                ChildId = childId,
                DenId = denId
            }).ToList();

            await client
                .From<EventChild>()
                .Insert(associations);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save event children");
        }
    }
}
