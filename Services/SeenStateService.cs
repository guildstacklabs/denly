using Denly.Models;
using Microsoft.Extensions.Logging;

namespace Denly.Services;

public class SeenStateService : SupabaseServiceBase, ISeenStateService
{
    private readonly ILogger<SeenStateService> _logger;

    public SeenStateService(IDenService denService, IAuthService authService, ILogger<SeenStateService> logger)
        : base(denService, authService)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, DateTime?>> GetSeenMapAsync(IEnumerable<string> eventIds, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = TryGetCurrentDenId();
        if (denId == null) return new Dictionary<string, DateTime?>();

        var ids = eventIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, DateTime?>();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await GetClientOrThrow()
                .From<EventSeenState>()
                .Select("event_id, last_seen_at")
                .Where(s => s.DenId == denId)
                .Filter("event_id", Supabase.Postgrest.Constants.Operator.In, ids)
                .Get();

            var map = response.Models
                .GroupBy(s => s.EventId)
                .ToDictionary(g => g.Key, g => (DateTime?)g.Max(x => x.LastSeenAt));

            foreach (var id in ids)
            {
                if (!map.ContainsKey(id))
                {
                    map[id] = null;
                }
            }

            return map;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get seen state map");
            return new Dictionary<string, DateTime?>();
        }
    }

    public async Task MarkSeenAsync(string eventId, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var denId = GetCurrentDenIdOrThrow();
        var userId = GetAuthenticatedUserIdOrThrow();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var existing = await GetClientOrThrow()
                .From<EventSeenState>()
                .Select("id, event_id, user_id, last_seen_at")
                .Where(s => s.EventId == eventId && s.UserId == userId)
                .Limit(1)
                .Get();

            var current = existing.Models.FirstOrDefault();

            if (current != null)
            {
                await GetClientOrThrow()
                    .From<EventSeenState>()
                    .Where(s => s.Id == current.Id)
                    .Set(s => s.LastSeenAt, updatedAt)
                    .Update();
            }
            else
            {
                var seen = new EventSeenState
                {
                    DenId = denId,
                    EventId = eventId,
                    UserId = userId,
                    LastSeenAt = updatedAt,
                    CreatedAt = DateTime.UtcNow
                };

                await GetClientOrThrow()
                    .From<EventSeenState>()
                    .Insert(seen);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark event as seen");
        }
    }

    public async Task<bool> IsUpdatedAsync(string eventId, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var userId = GetAuthenticatedUserIdOrThrow();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await GetClientOrThrow()
                .From<EventSeenState>()
                .Select("event_id, user_id, last_seen_at")
                .Where(s => s.EventId == eventId && s.UserId == userId)
                .Limit(1)
                .Get();

            var entry = response.Models.FirstOrDefault();
            return entry == null || updatedAt > entry.LastSeenAt;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check updated state");
            return false;
        }
    }
}
