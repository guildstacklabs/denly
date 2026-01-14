using Denly.Models;

namespace Denly.Services;

public interface IAuthService
{
    Task InitializeAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<AppUser?> GetCurrentUserAsync();
    Task<AuthResult> SignInWithGoogleAsync();
    Task<AuthResult> SignInWithEmailAsync(string email, string password);
    Task<AuthResult> SignUpWithEmailAsync(string email, string password);
    Task SignOutAsync();
    Task<bool> HasDenAsync();
    Task<Den?> GetCurrentDenAsync();
    Task CreateDenAsync(string denName);
    event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

    /// <summary>
    /// Gets the authenticated Supabase client for use by other services.
    /// </summary>
    Supabase.Client? GetSupabaseClient();
}

public record AuthResult(bool Success, string? ErrorMessage = null);

public class AuthStateChangedEventArgs : EventArgs
{
    public bool IsAuthenticated { get; }
    public AppUser? User { get; }

    public AuthStateChangedEventArgs(bool isAuthenticated, AppUser? user)
    {
        IsAuthenticated = isAuthenticated;
        User = user;
    }
}
