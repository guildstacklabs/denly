using Denly.Models;

namespace Denly.Services;

public interface IInviteService
{
    Task<DenInvite> CreateInviteAsync(string denId, string userId, string role = "co-parent");
    Task<DenInvite?> GetActiveInviteAsync(string denId);
    Task DeleteInviteAsync(string inviteId);
    Task<DenInvite?> ValidateInviteCodeAsync(string code);
    Task MarkInviteUsedAsync(string inviteId, string userId);
    Task<int> GetFailedAttemptsCountAsync(string userId, int minutes = 15);
    Task LogAttemptAsync(string userId, bool success);
}
