using System.Text.Json;
using Denly.Models;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Responses;

namespace Denly.Services;

public class SupabaseDenService : IDenService
{
    private const string CurrentDenStorageKey = "current_den_id";
    private const string InviteCodeCharset = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int InviteCodeLength = 8;
    private const int InviteExpirationDays = 3;
    private const int MaxFailedAttempts = 5;
    private const int RateLimitMinutes = 15;
    private static readonly TimeSpan MemberCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAuthService _authService;
    private readonly IClock _clock;
    private readonly ILogger<SupabaseDenService> _logger;
    private string? _currentDenId;
    private bool _isInitialized;
    private List<DenMember>? _cachedMembers;
    private string? _cachedMembersDenId;
    private DateTime _memberCacheUpdatedAtUtc;
    private Dictionary<string, Profile> _profileCache = new();
    private DateTime _profileCacheUpdatedAtUtc;

    public event EventHandler<DenChangedEventArgs>? DenChanged;

    // Use the authenticated client from AuthService
    private Supabase.Client? SupabaseClient => _authService.GetSupabaseClient();

    public SupabaseDenService(IAuthService authService, IClock clock, ILogger<SupabaseDenService> logger)
    {
        _authService = authService;
        _clock = clock;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogDebug("InitializeAsync - already initialized");
            return;
        }

        _logger.LogDebug("InitializeAsync starting");

        // Ensure auth service is initialized (which creates the authenticated client)
        await _authService.InitializeAsync();

        // Restore current den from secure storage
        try
        {
            var storedValue = await SecureStorage.GetAsync(CurrentDenStorageKey);
            _currentDenId = storedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring den from storage");
        }

        // Validate stored den belongs to current user, or find user's actual den
        if (!string.IsNullOrEmpty(_currentDenId))
        {
            var isValid = await ValidateUserDenMembershipAsync(_currentDenId);
            if (!isValid)
            {
                _logger.LogDebug("Stored den does not belong to current user, clearing");
                _currentDenId = null;
                SecureStorage.Remove(CurrentDenStorageKey);
            }
        }

        // If no valid den, check if user has any dens in database
        if (string.IsNullOrEmpty(_currentDenId))
        {
            await TryLoadUserDenAsync();
        }

        _isInitialized = true;
        _logger.LogDebug("InitializeAsync complete");
    }

    public Task ResetAsync()
    {
        _logger.LogDebug("ResetAsync - clearing state");
        _isInitialized = false;
        _currentDenId = null;
        ClearCaches();
        SecureStorage.Remove(CurrentDenStorageKey);
        return Task.CompletedTask;
    }

    private async Task TryLoadUserDenAsync()
    {
        try
        {
            var supabaseUser = SupabaseClient?.Auth.CurrentUser;
            if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
            {
                _logger.LogDebug("TryLoadUserDenAsync - no authenticated user, skipping");
                return;
            }

            var memberships = await SupabaseClient!
                .From<DenMember>()
                .Select("id, den_id, user_id, role, invited_by, joined_at")
                .Where(m => m.UserId == supabaseUser.Id)
                .Get();

            _logger.LogDebug("Found {Count} den memberships", memberships.Models.Count);

            if (memberships.Models.Count > 0)
            {
                var firstDenId = memberships.Models.First().DenId;
                await SetCurrentDenAsync(firstDenId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user dens");
        }
    }

    private async Task<bool> ValidateUserDenMembershipAsync(string denId)
    {
        try
        {
            var supabaseUser = SupabaseClient?.Auth.CurrentUser;
            if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
            {
                _logger.LogDebug("ValidateUserDenMembershipAsync - no authenticated user");
                return false;
            }

            var membership = await SupabaseClient!
                .From<DenMember>()
                .Select("id, den_id, user_id, role, invited_by, joined_at")
                .Where(m => m.UserId == supabaseUser.Id && m.DenId == denId)
                .Get();

            var isMember = membership.Models.Count > 0;
            _logger.LogDebug("ValidateUserDenMembershipAsync - result: {IsMember}", isMember);
            return isMember;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating den membership");
            return false;
        }
    }

    public string? GetCurrentDenId()
    {
        return _currentDenId;
    }

    public async Task<Den?> GetCurrentDenAsync()
    {
        if (string.IsNullOrEmpty(_currentDenId)) return null;

        try
        {
            var response = await SupabaseClient!
                .From<Den>()
                .Select("id, name, created_by, created_at")
                .Where(d => d.Id == _currentDenId)
                .Single();

            return response;
        }
        catch (PostgrestException ex) when (ex.StatusCode == 404 || ex.StatusCode == 406)
        {
            await HandleMissingCurrentDenAsync();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Den>> GetUserDensAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null) return new List<Den>();

        try
        {
            // Get all dens where user is a member
            var memberships = await SupabaseClient!
                .From<DenMember>()
                .Select("id, den_id, user_id, role, invited_by, joined_at")
                .Where(m => m.UserId == user.Id)
                .Get();

            if (memberships.Models.Count == 0)
                return new List<Den>();

            var denIds = memberships.Models.Select(m => m.DenId).ToList();

            var dens = await SupabaseClient!
                .From<Den>()
                .Select("id, name, created_by, created_at")
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, denIds)
                .Get();

            return dens.Models;
        }
        catch
        {
            return new List<Den>();
        }
    }

    public async Task SetCurrentDenAsync(string denId)
    {
        _logger.LogDebug("SetCurrentDenAsync called");
        _currentDenId = denId;
        ClearCaches();

        try
        {
            await SecureStorage.SetAsync(CurrentDenStorageKey, denId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving den to SecureStorage");
        }

        var den = await GetCurrentDenAsync();
        DenChanged?.Invoke(this, new DenChangedEventArgs(den));
    }

    public async Task<Den> CreateDenAsync(string name)
    {
        _logger.LogDebug("CreateDenAsync called");

        // Get user ID directly from the Supabase auth session
        // This ensures we use the same ID that auth.uid() sees on the server
        var supabaseUser = SupabaseClient?.Auth.CurrentUser;
        if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
        {
            _logger.LogWarning("CreateDenAsync - no authenticated session");
            throw new InvalidOperationException("User not authenticated");
        }

        var userId = supabaseUser.Id;

        var den = new Den
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            CreatedBy = userId,
            CreatedAt = _clock.UtcNow
        };

        // Insert den into Supabase
        try
        {
            _logger.LogDebug("Inserting den into Supabase");
            await SupabaseClient!
                .From<Den>()
                .Insert(den);
            _logger.LogDebug("Den inserted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert den");
            throw new InvalidOperationException($"Failed to create den: {ex.Message}", ex);
        }

        // Add owner as member
        var member = new DenMember
        {
            Id = Guid.NewGuid().ToString(),
            DenId = den.Id,
            UserId = userId,
            Role = "owner",
            JoinedAt = _clock.UtcNow
        };

        try
        {
            _logger.LogDebug("Inserting den_member into Supabase");
            await SupabaseClient!
                .From<DenMember>()
                .Insert(member);
            _logger.LogDebug("DenMember inserted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert den_member");
            // Try to clean up the orphaned den
            try
            {
                await SupabaseClient!.From<Den>().Where(d => d.Id == den.Id).Delete();
                _logger.LogDebug("Cleaned up orphaned den after member insert failure");
            }
            catch { /* Best effort cleanup */ }
            throw new InvalidOperationException($"Failed to add owner as member: {ex.Message}", ex);
        }

        // Set as current den
        await SetCurrentDenAsync(den.Id);
        _logger.LogInformation("Den created successfully");

        return den;
    }

    public async Task<List<DenMember>> GetDenMembersAsync(string? denId = null)
    {
        var targetDenId = denId ?? _currentDenId;

        if (string.IsNullOrEmpty(targetDenId))
        {
            return new List<DenMember>();
        }

        try
        {
            if (IsMemberCacheValid(targetDenId))
            {
                return _cachedMembers!
                    .Select(member => member.Clone())
                    .ToList();
            }

            // Fetch den members
            var response = await SupabaseClient!
                .From<DenMember>()
                .Select("id, den_id, user_id, role, invited_by, joined_at")
                .Where(m => m.DenId == targetDenId)
                .Get();

            var members = response.Models;
            _logger.LogDebug("Fetched {Count} members", members.Count);

            if (members.Count == 0)
                return members;

            // Fetch profiles for all member user IDs to get display names
            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var profiles = await GetProfilesAsync(userIds);
            _logger.LogDebug("Fetched {Count} profiles", profiles.Count);

            // Populate display properties on members
            foreach (var member in members)
            {
                if (profiles.TryGetValue(member.UserId, out var profile))
                {
                    member.Email = profile.Email;
                    member.DisplayName = profile.Name;
                    member.AvatarUrl = profile.AvatarUrl;
                }
            }

            StoreMemberCache(targetDenId, members);
            return members;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get den members");
            return new List<DenMember>();
        }
    }

    private bool IsMemberCacheValid(string denId)
    {
        if (_cachedMembers == null) return false;
        if (_cachedMembersDenId != denId) return false;
        return _clock.UtcNow - _memberCacheUpdatedAtUtc <= MemberCacheTtl;
    }

    private void StoreMemberCache(string denId, List<DenMember> members)
    {
        _cachedMembersDenId = denId;
        _memberCacheUpdatedAtUtc = _clock.UtcNow;
        _cachedMembers = members.Select(member => member.Clone()).ToList();
    }

    public void InvalidateMembersCache()
    {
        _cachedMembers = null;
        _cachedMembersDenId = null;
        _memberCacheUpdatedAtUtc = default;
        // Also clear profile cache as it's related
        _profileCache.Clear();
        _profileCacheUpdatedAtUtc = default;
        _logger.LogDebug("Member and profile caches invalidated");
    }

    private void ClearCaches()
    {
        InvalidateMembersCache();
    }

    private async Task HandleMissingCurrentDenAsync()
    {
        _currentDenId = null;
        ClearCaches();
        SecureStorage.Remove(CurrentDenStorageKey);
        await TryLoadUserDenAsync();
    }

    public async Task<Dictionary<string, Profile>> GetProfilesAsync(List<string> userIds)
    {
        if (userIds.Count == 0) return new Dictionary<string, Profile>();

        if (_clock.UtcNow - _profileCacheUpdatedAtUtc > MemberCacheTtl)
        {
            _profileCache.Clear();
            _profileCacheUpdatedAtUtc = _clock.UtcNow;
        }

        var missingIds = userIds.Where(id => !_profileCache.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            var profilesResponse = await SupabaseClient!
                .From<Profile>()
                .Select("id, email, name, avatar_url, created_at")
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, missingIds)
                .Get();

            foreach (var profile in profilesResponse.Models)
            {
                _profileCache[profile.Id] = profile;
            }
        }

        return _profileCache
            .Where(entry => userIds.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    public async Task RemoveMemberAsync(string denId, string userId)
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null) throw new InvalidOperationException("User not authenticated");

        // Check if current user is owner
        if (!await IsOwnerAsync(denId))
            throw new InvalidOperationException("Only the den owner can remove members");

        // Cannot remove owner - check member role
        var memberToRemove = await SupabaseClient!
            .From<DenMember>()
            .Select("id, den_id, user_id, role, invited_by, joined_at")
            .Where(m => m.DenId == denId && m.UserId == userId)
            .Single();

        if (memberToRemove?.Role == "owner")
            throw new InvalidOperationException("Cannot remove the den owner");

        await SupabaseClient!
            .From<DenMember>()
            .Where(m => m.DenId == denId && m.UserId == userId)
            .Delete();
        
        InvalidateMembersCache();
    }

    public async Task<bool> IsOwnerAsync(string? denId = null)
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null) return false;

        var targetDenId = denId ?? _currentDenId;
        if (string.IsNullOrEmpty(targetDenId)) return false;

        try
        {
            // Check if user has owner role in den_members
            var membership = await SupabaseClient!
                .From<DenMember>()
                .Select("id, den_id, user_id, role, invited_by, joined_at")
                .Where(m => m.DenId == targetDenId && m.UserId == user.Id)
                .Single();

            return membership?.Role == "owner";
        }
        catch
        {
            return false;
        }
    }

    public async Task<DenInvite> CreateInviteAsync(string? denId = null, string role = "co-parent")
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null) throw new InvalidOperationException("User not authenticated");

        var targetDenId = denId ?? _currentDenId;
        if (string.IsNullOrEmpty(targetDenId))
            throw new InvalidOperationException("No den selected");

        // Generate unique code
        var code = await GenerateUniqueCodeAsync();

        var invite = new DenInvite
        {
            Id = Guid.NewGuid().ToString(),
            DenId = targetDenId,
            Code = code,
            Role = role,
            CreatedBy = user.Id,
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddDays(InviteExpirationDays)
        };

        await SupabaseClient!
            .From<DenInvite>()
            .Insert(invite);

        return invite;
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

            // Check uniqueness
            try
            {
                var existing = await SupabaseClient!
                    .From<DenInvite>()
                    .Select("id, den_id, code, role, created_by, created_at, expires_at, used_by, used_at")
                    .Where(i => i.Code == code)
                    .Get();

                isUnique = existing.Models.Count == 0;
            }
            catch
            {
                isUnique = true; // Assume unique on error
            }
        } while (!isUnique);

        return code;
    }

    public async Task<DenInvite?> GetActiveInviteAsync(string? denId = null)
    {
        var targetDenId = denId ?? _currentDenId;
        if (string.IsNullOrEmpty(targetDenId)) return null;

        try
        {
            var response = await SupabaseClient!
                .From<DenInvite>()
                .Select("id, den_id, code, role, created_by, created_at, expires_at, used_by, used_at")
                .Where(i => i.DenId == targetDenId)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            // Find first valid (not expired, not used) invite
            return response.Models.FirstOrDefault(i => i.IsValid);
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteInviteAsync(string inviteId)
    {
        await SupabaseClient!
            .From<DenInvite>()
            .Where(i => i.Id == inviteId)
            .Delete();
    }

    public async Task<DenInvite?> ValidateInviteCodeAsync(string code)
    {
        var normalizedCode = NormalizeCode(code);

        try
        {
            var response = await SupabaseClient!
                .From<DenInvite>()
                .Select("id, den_id, code, role, created_by, created_at, expires_at, used_by, used_at")
                .Where(i => i.Code == normalizedCode)
                .Single();

            if (response == null || !response.IsValid)
                return null;

            // Get den name for display
            var den = await SupabaseClient!
                .From<Den>()
                .Select("id, name, created_by, created_at")
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

    public async Task<JoinDenResult> JoinDenAsync(string code)
    {
        try
        {
            _logger.LogDebug("JoinDenAsync starting");

            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogWarning("JoinDenAsync - user not authenticated");
                return new JoinDenResult(false, "User not authenticated");
            }

            // Check rate limit
            var failedAttempts = await GetFailedAttemptsCountAsync(RateLimitMinutes);
            _logger.LogDebug("JoinDenAsync - failed attempts: {Count}", failedAttempts);
            if (failedAttempts >= MaxFailedAttempts)
            {
                return new JoinDenResult(false, $"Too many attempts. Please try again later.", 0);
            }

            var normalizedCode = NormalizeCode(code);
            var invite = await ValidateInviteCodeAsync(normalizedCode);

            // Log attempt
            await LogAttemptAsync(user.Id, invite != null);

            if (invite == null)
            {
                _logger.LogDebug("JoinDenAsync - invite not found or invalid");
                var remaining = MaxFailedAttempts - failedAttempts - 1;
                return new JoinDenResult(false, "Invalid or expired code", remaining);
            }
            _logger.LogDebug("JoinDenAsync - invite valid");

            // Check if already a member
            var existingMember = await SupabaseClient!
                .From<DenMember>()
                .Select("id, den_id, user_id, role, invited_by, joined_at")
                .Where(m => m.DenId == invite.DenId && m.UserId == user.Id)
                .Get();

            if (existingMember.Models.Count > 0)
            {
                _logger.LogDebug("JoinDenAsync - user already a member, setting as current");
                // Already a member, just set as current
                await SetCurrentDenAsync(invite.DenId);
                var existingDen = await GetCurrentDenAsync();
                return new JoinDenResult(true, Den: existingDen);
            }

            _logger.LogDebug("JoinDenAsync - adding user as new member");
            // Add as member with role from invite
            var member = new DenMember
            {
                Id = Guid.NewGuid().ToString(),
                DenId = invite.DenId,
                UserId = user.Id,
                Role = invite.Role ?? "co-parent",
                InvitedBy = invite.CreatedBy,
                JoinedAt = _clock.UtcNow
            };

            await SupabaseClient!
                .From<DenMember>()
                .Insert(member);
            _logger.LogDebug("Member inserted successfully");
            InvalidateMembersCache();

            // Mark invite as used
            invite.UsedAt = _clock.UtcNow;
            invite.UsedBy = user.Id;

            await SupabaseClient!
                .From<DenInvite>()
                .Where(i => i.Id == invite.Id)
                .Set(i => i.UsedAt!, invite.UsedAt)
                .Set(i => i.UsedBy!, invite.UsedBy)
                .Update();

            // Set as current den
            await SetCurrentDenAsync(invite.DenId);
            var den = await GetCurrentDenAsync();
            _logger.LogInformation("User joined den successfully");

            return new JoinDenResult(true, Den: den);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join den");
            return new JoinDenResult(false, $"Failed to join den: {ex.Message}");
        }
    }

    public async Task<int> GetFailedAttemptsCountAsync(int minutes = 15)
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null) return 0;

        try
        {
            var cutoff = _clock.UtcNow.AddMinutes(-minutes);

            var response = await SupabaseClient!
                .From<InviteAttempt>()
                .Select("id, user_id, attempted_at, success")
                .Where(a => a.UserId == user.Id && !a.Success)
                .Filter("attempted_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, cutoff.ToString("o"))
                .Get();

            return response.Models.Count;
        }
        catch
        {
            return 0;
        }
    }

    private async Task LogAttemptAsync(string userId, bool success)
    {
        try
        {
            var attempt = new InviteAttempt
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Success = success,
                AttemptedAt = _clock.UtcNow
            };

            await SupabaseClient!
                .From<InviteAttempt>()
                .Insert(attempt);
        }
        catch
        {
            // Best effort logging
        }
    }

    private static string NormalizeCode(string code)
    {
        // Remove spaces, dashes, and convert to uppercase
        return code.Replace(" ", "").Replace("-", "").ToUpperInvariant();
    }

    public async Task<List<Child>> GetChildrenAsync()
    {
        var denId = _currentDenId;
        if (string.IsNullOrEmpty(denId))
        {
            return new List<Child>();
        }

        try
        {
            var response = await SupabaseClient!
                .From<Child>()
                .Select("id, den_id, name, birth_date, color, doctor_name, doctor_contact, allergies, school_name, clothing_size, shoe_size, created_at")
                .Where(c => c.DenId == denId)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get children for den {DenId}", denId);
            return new List<Child>();
        }
    }

    public async Task UpdateChildAsync(Child child)
    {
        try
        {
            // Use Upsert to handle both create and update
            await SupabaseClient!
                .From<Child>()
                .Upsert(child);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update child {ChildId}", child.Id);
            // Optionally re-throw or handle
        }
    }
}
