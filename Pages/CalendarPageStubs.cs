using Denly.Models;
using Denly.Services;

namespace Denly.Pages;

// Lightweight stubs to keep preview/compile safe when DI is not set.
internal sealed class StubScheduleService : IScheduleService
{
    public Task<List<Event>> GetEventsByMonthAsync(int year, int month, CancellationToken cancellationToken = default) => Task.FromResult(new List<Event>());
    public Task<List<Event>> GetEventsByDateAsync(DateTime date, CancellationToken cancellationToken = default) => Task.FromResult(new List<Event>());
    public Task<List<Event>> GetEventsByRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => Task.FromResult(new List<Event>());
    public Task<List<Event>> GetUpcomingEventsAsync(int count, CancellationToken cancellationToken = default) => Task.FromResult(new List<Event>());
    public Task<Event?> GetEventByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<Event?>(null);
    public Task SaveEventAsync(Event evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteEventAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> HasUpcomingEventsAsync() => Task.FromResult(false);
    public Task<string> GetOrCreateSubscriptionUrlAsync(CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
    public Task<List<EventChild>> GetEventChildrenAsync(IEnumerable<string> eventIds, CancellationToken cancellationToken = default) => Task.FromResult(new List<EventChild>());
    public Task SaveEventChildrenAsync(string eventId, List<string> childIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class StubChildService : IChildService
{
    public Task<List<Child>> GetActiveChildrenAsync() => Task.FromResult(new List<Child>());
    public Task<List<Child>> GetAllChildrenAsync() => Task.FromResult(new List<Child>());
    public Task<Child?> GetChildAsync(string childId) => Task.FromResult<Child?>(null);
    public Task<Child> AddChildAsync(Child child) => Task.FromResult(child);
    public Task UpdateChildAsync(Child child) => Task.CompletedTask;
    public Task DeactivateChildAsync(string childId) => Task.CompletedTask;
    public Task ReactivateChildAsync(string childId) => Task.CompletedTask;
    public Task<ChildNameValidationResult> ValidateChildNameAsync(Child child, string? excludeChildId = null) => Task.FromResult(new ChildNameValidationResult(true));
    public string GetDisplayName(Child child, IEnumerable<Child> allChildren) => child.FirstName;
    public Dictionary<string, string> GetDisplayNames(IEnumerable<Child> children) => new();
}

internal sealed class StubSeenStateService : ISeenStateService
{
    public Task<Dictionary<string, DateTime?>> GetSeenMapAsync(IEnumerable<string> eventIds, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<string, DateTime?>());
    public Task MarkSeenAsync(string eventId, DateTime updatedAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> IsUpdatedAsync(string eventId, DateTime updatedAt, CancellationToken cancellationToken = default) => Task.FromResult(false);
}

internal sealed class CalendarFallbackDenService : IDenService
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task ResetAsync() => Task.CompletedTask;
    public Task<Den?> GetCurrentDenAsync() => Task.FromResult<Den?>(null);
    public Task<List<Den>> GetUserDensAsync() => Task.FromResult(new List<Den>());
    public Task SetCurrentDenAsync(string denId) => Task.CompletedTask;
    public Task<Den> CreateDenAsync(string name) => Task.FromResult(new Den());
    public string? GetCurrentDenId() => null;
    public Task<List<DenMember>> GetDenMembersAsync(string? denId = null) => Task.FromResult(new List<DenMember>());
    public Task RemoveMemberAsync(string denId, string userId) => Task.CompletedTask;
    public Task<bool> IsOwnerAsync(string? denId = null) => Task.FromResult(false);
    public Task<Dictionary<string, Profile>> GetProfilesAsync(List<string> userIds) => Task.FromResult(new Dictionary<string, Profile>());
    public Task<DenInvite> CreateInviteAsync(string? denId = null, string role = "co-parent") => Task.FromResult(new DenInvite());
    public Task<DenInvite?> GetActiveInviteAsync(string? denId = null) => Task.FromResult<DenInvite?>(null);
    public Task DeleteInviteAsync(string inviteId) => Task.CompletedTask;
    public Task<DenInvite?> ValidateInviteCodeAsync(string code) => Task.FromResult<DenInvite?>(null);
    public Task<JoinDenResult> JoinDenAsync(string code) => Task.FromResult(new JoinDenResult(false));
    public Task<int> GetFailedAttemptsCountAsync(int minutes = 15) => Task.FromResult(0);
    public event EventHandler<DenChangedEventArgs>? DenChanged;
    public Task<List<Child>> GetChildrenAsync() => Task.FromResult(new List<Child>());
    public Task UpdateChildAsync(Child child) => Task.CompletedTask;
}
