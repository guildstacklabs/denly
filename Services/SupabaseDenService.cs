using System.Text.Json;
using Denly.Models;
using Supabase;
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

    public SupabaseDenService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            Console.WriteLine($"[DenService] InitializeAsync - already initialized, currentDenId: {_currentDenId ?? "null"}");
            return;
        }

        Console.WriteLine("[DenService] InitializeAsync - starting initialization");

        // Ensure auth service is initialized (which creates the authenticated client)
        await _authService.InitializeAsync();

        // Restore current den from secure storage
        try
        {
            Console.WriteLine($"[DenService] InitializeAsync - reading from SecureStorage key: '{CurrentDenStorageKey}'");
            var storedValue = await SecureStorage.GetAsync(CurrentDenStorageKey);
            Console.WriteLine($"[DenService] InitializeAsync - SecureStorage returned: '{storedValue ?? "null"}' (length: {storedValue?.Length ?? 0})");
            _currentDenId = storedValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DenService] InitializeAsync - error restoring from storage: {ex.Message}");
            Console.WriteLine($"[DenService] InitializeAsync - exception type: {ex.GetType().Name}");
        }

        // Validate stored den belongs to current user, or find user's actual den
        if (!string.IsNullOrEmpty(_currentDenId))
        {
            Console.WriteLine("[DenService] InitializeAsync - validating stored den belongs to current user...");
            var isValid = await ValidateUserDenMembershipAsync(_currentDenId);
            if (!isValid)
            {
                Console.WriteLine("[DenService] InitializeAsync - stored den does not belong to current user, clearing...");
                _currentDenId = null;
                SecureStorage.Remove(CurrentDenStorageKey);
            }
        }

        // If no valid den, check if user has any dens in database
        if (string.IsNullOrEmpty(_currentDenId))
        {
            Console.WriteLine("[DenService] InitializeAsync - no valid den, checking database for user's dens...");
            await TryLoadUserDenAsync();
        }

        _isInitialized = true;
        Console.WriteLine($"[DenService] InitializeAsync - complete, currentDenId: {_currentDenId ?? "null"}");
    }

    public Task ResetAsync()
    {
        Console.WriteLine("[DenService] ResetAsync - clearing state");
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
                Console.WriteLine("[DenService] TryLoadUserDenAsync - no authenticated user, skipping");
                return;
            }

            Console.WriteLine($"[DenService] TryLoadUserDenAsync - checking dens for user: {supabaseUser.Id}");

            var memberships = await SupabaseClient!
                .From<DenMember>()
                .Where(m => m.UserId == supabaseUser.Id)
                .Get();

            Console.WriteLine($"[DenService] TryLoadUserDenAsync - found {memberships.Models.Count} den memberships");

            if (memberships.Models.Count > 0)
            {
                var firstDenId = memberships.Models.First().DenId;
                Console.WriteLine($"[DenService] TryLoadUserDenAsync - setting current den to: {firstDenId}");
                await SetCurrentDenAsync(firstDenId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DenService] TryLoadUserDenAsync - error: {ex.Message}");
        }
    }

    private async Task<bool> ValidateUserDenMembershipAsync(string denId)
    {
        try
        {
            var supabaseUser = SupabaseClient?.Auth.CurrentUser;
            if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
            {
                Console.WriteLine("[DenService] ValidateUserDenMembershipAsync - no authenticated user");
                return false;
            }

            Console.WriteLine($"[DenService] ValidateUserDenMembershipAsync - checking if user {supabaseUser.Id} is member of den {denId}");

            var membership = await SupabaseClient!
                .From<DenMember>()
                .Where(m => m.UserId == supabaseUser.Id && m.DenId == denId)
                .Get();

            var isMember = membership.Models.Count > 0;
            Console.WriteLine($"[DenService] ValidateUserDenMembershipAsync - result: {isMember}");
            return isMember;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DenService] ValidateUserDenMembershipAsync - error: {ex.Message}");
            return false;
        }
    }

    public string? GetCurrentDenId()
    {
        Console.WriteLine($"[DenService] GetCurrentDenId called - isInitialized: {_isInitialized}, currentDenId: {_currentDenId ?? "null"}");
        return _currentDenId;
    }

    public async Task<Den?> GetCurrentDenAsync()
    {
        if (string.IsNullOrEmpty(_currentDenId)) return null;

        try
        {
            var response = await SupabaseClient!
                .From<Den>()
                .Where(d => d.Id == _currentDenId)
                .Single();

            return response;
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
                .Where(m => m.UserId == user.Id)
                .Get();

            if (memberships.Models.Count == 0)
                return new List<Den>();

            var denIds = memberships.Models.Select(m => m.DenId).ToList();

            var dens = await SupabaseClient!
                .From<Den>()
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
        Console.WriteLine($"[DenService] SetCurrentDenAsync called with denId: {denId}");
        _currentDenId = denId;
        ClearCaches();

        try
        {
            Console.WriteLine($"[DenService] SetCurrentDenAsync - writing to SecureStorage key: '{CurrentDenStorageKey}' value: '{denId}'");
            await SecureStorage.SetAsync(CurrentDenStorageKey, denId);

            // Verify the write by reading back
            var verifyValue = await SecureStorage.GetAsync(CurrentDenStorageKey);
            Console.WriteLine($"[DenService] SetCurrentDenAsync - verification read: '{verifyValue ?? "null"}'");

            if (verifyValue == denId)
            {
                Console.WriteLine($"[DenService] SetCurrentDenAsync - SecureStorage write VERIFIED successfully");
            }
            else
            {
                Console.WriteLine($"[DenService] SetCurrentDenAsync - WARNING: SecureStorage write verification FAILED! Expected '{denId}', got '{verifyValue ?? "null"}'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DenService] SetCurrentDenAsync - error saving to SecureStorage: {ex.Message}");
            Console.WriteLine($"[DenService] SetCurrentDenAsync - exception type: {ex.GetType().Name}");
        }

        var den = await GetCurrentDenAsync();
        Console.WriteLine($"[DenService] SetCurrentDenAsync - firing DenChanged event, den name: {den?.Name ?? "null"}");
        DenChanged?.Invoke(this, new DenChangedEventArgs(den));
    }

    public async Task<Den> CreateDenAsync(string name)
    {
        Console.WriteLine($"[SupabaseDenService] CreateDenAsync called with name: {name}");

        // Get user ID directly from the Supabase auth session
        // This ensures we use the same ID that auth.uid() sees on the server
        var supabaseUser = SupabaseClient?.Auth.CurrentUser;
        if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
        {
            Console.WriteLine("[SupabaseDenService] Error: No authenticated Supabase session");
            throw new InvalidOperationException("User not authenticated");
        }

        var userId = supabaseUser.Id;
        Console.WriteLine($"[SupabaseDenService] Supabase auth.uid(): {userId}");
        Console.WriteLine($"[SupabaseDenService] Session access token present: {!string.IsNullOrEmpty(SupabaseClient?.Auth.CurrentSession?.AccessToken)}");

        var den = new Den
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        Console.WriteLine($"[SupabaseDenService] Created Den object - ID: {den.Id}, CreatedBy: {den.CreatedBy}");

        // Insert den into Supabase
        try
        {
            Console.WriteLine("[SupabaseDenService] Inserting den into Supabase...");
            var denResponse = await SupabaseClient!
                .From<Den>()
                .Insert(den);
            Console.WriteLine($"[SupabaseDenService] Den insert response: {denResponse?.Models?.Count ?? 0} models returned");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseDenService] Error inserting den: {ex.Message}");
            Console.WriteLine($"[SupabaseDenService] Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException($"Failed to create den: {ex.Message}", ex);
        }

        // Add owner as member
        var member = new DenMember
        {
            Id = Guid.NewGuid().ToString(),
            DenId = den.Id,
            UserId = userId,
            Role = "owner",
            JoinedAt = DateTime.UtcNow
        };
        Console.WriteLine($"[SupabaseDenService] Created DenMember object with ID: {member.Id}");

        try
        {
            Console.WriteLine("[SupabaseDenService] Inserting den_member into Supabase...");
            var memberResponse = await SupabaseClient!
                .From<DenMember>()
                .Insert(member);
            Console.WriteLine($"[SupabaseDenService] DenMember insert response: {memberResponse?.Models?.Count ?? 0} models returned");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseDenService] Error inserting den_member: {ex.Message}");
            Console.WriteLine($"[SupabaseDenService] Stack trace: {ex.StackTrace}");
            // Try to clean up the orphaned den
            try
            {
                await SupabaseClient!.From<Den>().Where(d => d.Id == den.Id).Delete();
                Console.WriteLine("[SupabaseDenService] Cleaned up orphaned den after member insert failure");
            }
            catch { /* Best effort cleanup */ }
            throw new InvalidOperationException($"Failed to add owner as member: {ex.Message}", ex);
        }

        // Set as current den
        await SetCurrentDenAsync(den.Id);
        Console.WriteLine($"[SupabaseDenService] Successfully created den and set as current: {den.Id}");

        return den;
    }

    public async Task<List<DenMember>> GetDenMembersAsync(string? denId = null)
    {
        var targetDenId = denId ?? _currentDenId;
        Console.WriteLine($"[DenService] GetDenMembersAsync called, targetDenId: {targetDenId ?? "null"}");

        if (string.IsNullOrEmpty(targetDenId))
        {
            Console.WriteLine("[DenService] GetDenMembersAsync - no den ID, returning empty list");
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
                .Where(m => m.DenId == targetDenId)
                .Get();

            var members = response.Models;
            Console.WriteLine($"[DenService] GetDenMembersAsync - fetched {members.Count} members from den_members table");

            if (members.Count == 0)
                return members;

            // Fetch profiles for all member user IDs to get display names
            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            Console.WriteLine($"[DenService] GetDenMembersAsync - fetching profiles for {userIds.Count} user IDs");

            var profiles = await GetProfilesAsync(userIds);
            Console.WriteLine($"[DenService] GetDenMembersAsync - fetched {profiles.Count} profiles");

            // Populate display properties on members
            foreach (var member in members)
            {
                if (profiles.TryGetValue(member.UserId, out var profile))
                {
                    member.Email = profile.Email;
                    member.DisplayName = profile.Name;
                    member.AvatarUrl = profile.AvatarUrl;
                    Console.WriteLine($"[DenService] GetDenMembersAsync - member {member.UserId}: Email={profile.Email}, Name={profile.Name}");
                }
                else
                {
                    Console.WriteLine($"[DenService] GetDenMembersAsync - no profile found for member {member.UserId}");
                }
            }

            StoreMemberCache(targetDenId, members);
            return members;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DenService] GetDenMembersAsync - error: {ex.Message}");
            return new List<DenMember>();
        }
    }

    private bool IsMemberCacheValid(string denId)
    {
        if (_cachedMembers == null) return false;
        if (_cachedMembersDenId != denId) return false;
        return DateTime.UtcNow - _memberCacheUpdatedAtUtc <= MemberCacheTtl;
    }

    private void StoreMemberCache(string denId, List<DenMember> members)
    {
        _cachedMembersDenId = denId;
        _memberCacheUpdatedAtUtc = DateTime.UtcNow;
        _cachedMembers = members.Select(member => member.Clone()).ToList();
    }

    private void ClearCaches()
    {
        _cachedMembers = null;
        _cachedMembersDenId = null;
        _profileCache.Clear();
        _memberCacheUpdatedAtUtc = default;
        _profileCacheUpdatedAtUtc = default;
    }

    private async Task<Dictionary<string, Profile>> GetProfilesAsync(List<string> userIds)
    {
        if (userIds.Count == 0) return new Dictionary<string, Profile>();

        if (DateTime.UtcNow - _profileCacheUpdatedAtUtc > MemberCacheTtl)
        {
            _profileCache.Clear();
            _profileCacheUpdatedAtUtc = DateTime.UtcNow;
        }

        var missingIds = userIds.Where(id => !_profileCache.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            var profilesResponse = await SupabaseClient!
                .From<Profile>()
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
            .Where(m => m.DenId == denId && m.UserId == userId)
            .Single();

        if (memberToRemove?.Role == "owner")
            throw new InvalidOperationException("Cannot remove the den owner");

        await SupabaseClient!
            .From<DenMember>()
            .Where(m => m.DenId == denId && m.UserId == userId)
            .Delete();
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
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(InviteExpirationDays)
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
                .Where(i => i.Code == normalizedCode)
                .Single();

            if (response == null || !response.IsValid)
                return null;

            // Get den name for display
            var den = await SupabaseClient!
                .From<Den>()
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
            Console.WriteLine($"[DenService] JoinDenAsync - starting with code: {code}");

            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                Console.WriteLine("[DenService] JoinDenAsync - user not authenticated");
                return new JoinDenResult(false, "User not authenticated");
            }
            Console.WriteLine($"[DenService] JoinDenAsync - user: {user.Id}");

            // Check rate limit
            var failedAttempts = await GetFailedAttemptsCountAsync(RateLimitMinutes);
            Console.WriteLine($"[DenService] JoinDenAsync - failed attempts: {failedAttempts}");
            if (failedAttempts >= MaxFailedAttempts)
            {
                return new JoinDenResult(false, $"Too many attempts. Please try again later.", 0);
            }

            var normalizedCode = NormalizeCode(code);
            Console.WriteLine($"[DenService] JoinDenAsync - normalized code: {normalizedCode}");
            var invite = await ValidateInviteCodeAsync(normalizedCode);

            // Log attempt
            await LogAttemptAsync(user.Id, invite != null);

            if (invite == null)
            {
                Console.WriteLine("[DenService] JoinDenAsync - invite not found or invalid");
                var remaining = MaxFailedAttempts - failedAttempts - 1;
                return new JoinDenResult(false, "Invalid or expired code", remaining);
            }
            Console.WriteLine($"[DenService] JoinDenAsync - invite valid, denId: {invite.DenId}");

            // Check if already a member
            var existingMember = await SupabaseClient!
                .From<DenMember>()
                .Where(m => m.DenId == invite.DenId && m.UserId == user.Id)
                .Get();

            if (existingMember.Models.Count > 0)
            {
                Console.WriteLine("[DenService] JoinDenAsync - user already a member, setting as current");
                // Already a member, just set as current
                await SetCurrentDenAsync(invite.DenId);
                var existingDen = await GetCurrentDenAsync();
                return new JoinDenResult(true, Den: existingDen);
            }

            Console.WriteLine("[DenService] JoinDenAsync - adding user as new member");
            // Add as member with role from invite
            var member = new DenMember
            {
                Id = Guid.NewGuid().ToString(),
                DenId = invite.DenId,
                UserId = user.Id,
                Role = invite.Role ?? "co-parent",
                InvitedBy = invite.CreatedBy,
                JoinedAt = DateTime.UtcNow
            };

            await SupabaseClient!
                .From<DenMember>()
                .Insert(member);
            Console.WriteLine("[DenService] JoinDenAsync - member inserted successfully");

            // Mark invite as used
            Console.WriteLine("[DenService] JoinDenAsync - marking invite as used");
            invite.UsedAt = DateTime.UtcNow;
            invite.UsedBy = user.Id;

            await SupabaseClient!
                .From<DenInvite>()
                .Where(i => i.Id == invite.Id)
                .Set(i => i.UsedAt!, invite.UsedAt)
                .Set(i => i.UsedBy!, invite.UsedBy)
                .Update();
            Console.WriteLine("[DenService] JoinDenAsync - invite updated");

            // Set as current den
            Console.WriteLine("[DenService] JoinDenAsync - setting current den");
            await SetCurrentDenAsync(invite.DenId);
            var den = await GetCurrentDenAsync();
            Console.WriteLine($"[DenService] JoinDenAsync - complete, den: {den?.Name}");

            return new JoinDenResult(true, Den: den);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DenService] JoinDenAsync - ERROR: {ex.Message}");
            Console.WriteLine($"[DenService] JoinDenAsync - Stack: {ex.StackTrace}");
            return new JoinDenResult(false, $"Failed to join den: {ex.Message}");
        }
    }

    public async Task<int> GetFailedAttemptsCountAsync(int minutes = 15)
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null) return 0;

        try
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

            var response = await SupabaseClient!
                .From<InviteAttempt>()
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
                AttemptedAt = DateTime.UtcNow
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
}
