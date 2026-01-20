using Supabase;

namespace Denly.Services;

public abstract class SupabaseServiceBase
{
    private bool _isInitialized;

    protected SupabaseServiceBase(IDenService denService, IAuthService authService)
    {
        DenService = denService;
        AuthService = authService;
    }

    protected IDenService DenService { get; }

    protected IAuthService AuthService { get; }

    protected Client? SupabaseClient => AuthService.GetSupabaseClient();

    protected async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        await AuthService.InitializeAsync();
        await DenService.InitializeAsync();
        _isInitialized = true;
    }

    /// <summary>
    /// Gets the current den ID or throws if no den is selected.
    /// Use for write operations that require a den context.
    /// </summary>
    protected string GetCurrentDenIdOrThrow()
    {
        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            throw new InvalidOperationException("No den selected");
        }
        return denId;
    }

    /// <summary>
    /// Gets the current den ID or returns null if no den is selected.
    /// Use for read operations that should return empty results when no den.
    /// </summary>
    protected string? TryGetCurrentDenId()
    {
        var denId = DenService.GetCurrentDenId();
        return string.IsNullOrEmpty(denId) ? null : denId;
    }

    /// <summary>
    /// Gets the authenticated user ID or throws if not authenticated.
    /// Use for write operations that require user context.
    /// </summary>
    protected string GetAuthenticatedUserIdOrThrow()
    {
        var user = SupabaseClient?.Auth.CurrentUser;
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            throw new InvalidOperationException("User not authenticated");
        }
        return user.Id;
    }

    /// <summary>
    /// Gets the Supabase client or throws if not available.
    /// Eliminates null-forgiving operator usage.
    /// </summary>
    protected Client GetClientOrThrow()
    {
        var client = SupabaseClient;
        if (client == null)
        {
            throw new InvalidOperationException("Supabase client not initialized");
        }
        return client;
    }
}
