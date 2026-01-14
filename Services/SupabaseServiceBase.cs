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
}
