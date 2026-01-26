using Denly.Models;

namespace Denly.Services;

public interface IDenService
{
    // Initialization
    Task InitializeAsync();
    Task ResetAsync();

    // Den management
    Task<Den?> GetCurrentDenAsync();
    Task<List<Den>> GetUserDensAsync();
    Task SetCurrentDenAsync(string denId);
    Task<Den> CreateDenAsync(string name);
    string? GetCurrentDenId();

    // Members
    Task<List<DenMember>> GetDenMembersAsync(string? denId = null);
    Task RemoveMemberAsync(string denId, string userId);
    Task<bool> IsOwnerAsync(string? denId = null);

    // Profile lookup (cached)
    Task<Dictionary<string, Profile>> GetProfilesAsync(List<string> userIds);

    // Joining
    Task<JoinDenResult> JoinDenAsync(string code);

    // Events
    event EventHandler<DenChangedEventArgs>? DenChanged;
}

public record JoinDenResult(
    bool Success,
    string? Error = null,
    int AttemptsRemaining = 5,
    Den? Den = null
);

public class DenChangedEventArgs : EventArgs
{
    public Den? Den { get; }

    public DenChangedEventArgs(Den? den)
    {
        Den = den;
    }
}
