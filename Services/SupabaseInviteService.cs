using Denly.Models;
using Microsoft.Extensions.Logging;
using Supabase;

namespace Denly.Services;

public class SupabaseInviteService : IInviteService
{
    private const string InviteCodeCharset = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int InviteCodeLength = 8;
    private const int InviteExpirationDays = 3;

    private readonly IAuthService _authService;
    private readonly IClock _clock;
    private readonly ILogger<SupabaseInviteService> _logger;

    private Client? SupabaseClient => _authService.GetSupabaseClient();

    public SupabaseInviteService(IAuthService authService, IClock clock, ILogger<SupabaseInviteService> logger)
    {
        _authService = authService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<DenInvite> CreateInviteAsync(string denId, string userId, string role = "co-parent")
    {
        if (SupabaseClient == null) throw new InvalidOperationException("Client not initialized");

        var code = await GenerateUniqueCodeAsync();

        var invite = new DenInvite
        {
            Id = Guid.NewGuid().ToString(),
            DenId = denId,
            Code = code,
            Role = role,
            CreatedBy = userId,
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddDays(InviteExpirationDays)
        };

        await SupabaseClient
            .From<DenInvite>()
            .Insert(invite);

        return invite;
    }

    public async Task<DenInvite?> GetActiveInviteAsync(string denId)
    {
        if (SupabaseClient == null) return null;

        try
        {
            var response = await SupabaseClient
                .From<DenInvite>()
                .Select("id, den_id, code, role, created_by, created_at, expires_at, used_by, used_at")
                .Where(i => i.DenId == denId)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            return response.Models.FirstOrDefault(i => i.IsValid);
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteInviteAsync(string inviteId)
    {
        if (SupabaseClient == null) return;

        await SupabaseClient
            .From<DenInvite>()
            .Where(i => i.Id == inviteId)
            .Delete();
    }

    public async Task<DenInvite?> ValidateInviteCodeAsync(string code)
    {
        if (SupabaseClient == null) return null;

        var normalizedCode = NormalizeCode(code);

        try
        {
            var response = await SupabaseClient
                .From<DenInvite>()
                .Select("id, den_id, code, role, created_by, created_at, expires_at, used_by, used_at")
                .Where(i => i.Code == normalizedCode)
                .Single();

            if (response == null || !response.IsValid)
                return null;

            // Get den name
            var den = await SupabaseClient
                .From<Den>()
                .Select("id, name")
                .Where(d => d.Id == response.DenId)
                .Single();

            response.DenName = den?.Name;

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task MarkInviteUsedAsync(string inviteId, string userId)
    {
        if (SupabaseClient == null) return;

        await SupabaseClient
            .From<DenInvite>()
            .Where(i => i.Id == inviteId)
            .Set(i => i.UsedAt!, _clock.UtcNow)
            .Set(i => i.UsedBy!, userId)
            .Update();
    }

    public async Task<int> GetFailedAttemptsCountAsync(string userId, int minutes = 15)
    {
        if (SupabaseClient == null) return 0;

        try
        {
            var cutoff = _clock.UtcNow.AddMinutes(-minutes);

            var response = await SupabaseClient
                .From<InviteAttempt>()
                .Select("id, user_id, attempted_at, success")
                .Where(a => a.UserId == userId && !a.Success)
                .Filter("attempted_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, cutoff.ToString("o"))
                .Get();

            return response.Models.Count;
        }
        catch
        {
            return 0;
        }
    }

    public async Task LogAttemptAsync(string userId, bool success)
    {
        if (SupabaseClient == null) return;

        try
        {
            var attempt = new InviteAttempt
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Success = success,
                AttemptedAt = _clock.UtcNow
            };

            await SupabaseClient
                .From<InviteAttempt>()
                .Insert(attempt);
        }
        catch
        {
            // Best effort
        }
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        var random = new Random();
        string code;
        bool isUnique;

        do
        {
            var chars = new char[InviteCodeLength];
            for (int i = 0; i < InviteCodeLength; i++)
            {
                chars[i] = InviteCodeCharset[random.Next(InviteCodeCharset.Length)];
            }
            code = new string(chars);

            try
            {
                var existing = await SupabaseClient!
                    .From<DenInvite>()
                    .Select("id")
                    .Where(i => i.Code == code)
                    .Get();

                isUnique = existing.Models.Count == 0;
            }
            catch
            {
                isUnique = true;
            }
        } while (!isUnique);

        return code;
    }

    private static string NormalizeCode(string code)
    {
        return code.Replace(" ", "").Replace("-", "").ToUpperInvariant();
    }
}
