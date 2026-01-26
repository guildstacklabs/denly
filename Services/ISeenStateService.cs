namespace Denly.Services;

public interface ISeenStateService
{
    Task<Dictionary<string, DateTime?>> GetSeenMapAsync(IEnumerable<string> eventIds, CancellationToken cancellationToken = default);
    Task MarkSeenAsync(string eventId, DateTime updatedAt, CancellationToken cancellationToken = default);
    Task<bool> IsUpdatedAsync(string eventId, DateTime updatedAt, CancellationToken cancellationToken = default);
}
