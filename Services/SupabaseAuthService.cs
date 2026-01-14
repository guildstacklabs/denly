using System.Text.Json;
using Denly.Models;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Denly.Services;

public class SupabaseAuthService : IAuthService
{
    private const string SupabaseUrl = "https://fzzrciqjdboiqxamhfzp.supabase.co";
    private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ6enJjaXFqZGJvaXF4YW1oZnpwIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Njc4MDM0NjcsImV4cCI6MjA4MzM3OTQ2N30.k72rZhuj2fUaSGrd8MeMR8Ugz2EkFShlrNV8W8nfvO8";
    private const string CallbackUrl = "com.companyname.denly://login-callback";
    private const string SessionStorageKey = "supabase_session";

    private readonly IServiceProvider _serviceProvider;
    private Supabase.Client? _supabase;
    private bool _isInitialized;

    public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

    public SupabaseAuthService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _supabase = new Supabase.Client(SupabaseUrl, SupabaseAnonKey, options);
        await _supabase.InitializeAsync();

        // Try to restore session from secure storage
        await RestoreSessionAsync();

        // Listen for auth state changes
        _supabase.Auth.AddStateChangedListener((sender, state) =>
        {
            var isAuth = state == Constants.AuthState.SignedIn;
            var user = isAuth ? MapToAppUser(_supabase.Auth.CurrentUser) : null;
            AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(isAuth, user));
        });

        _isInitialized = true;
    }

    private async Task RestoreSessionAsync()
    {
        try
        {
            var sessionJson = await SecureStorage.GetAsync(SessionStorageKey);
            if (!string.IsNullOrEmpty(sessionJson))
            {
                var session = JsonSerializer.Deserialize<Session>(sessionJson);
                if (session != null)
                {
                    await _supabase!.Auth.SetSession(session.AccessToken!, session.RefreshToken!);
                }
            }
        }
        catch
        {
            // Session restore failed, user will need to log in again
        }
    }

    private async Task PersistSessionAsync()
    {
        try
        {
            var session = _supabase?.Auth.CurrentSession;
            if (session != null)
            {
                var sessionJson = JsonSerializer.Serialize(session);
                await SecureStorage.SetAsync(SessionStorageKey, sessionJson);
            }
        }
        catch
        {
            // Secure storage not available
        }
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(_supabase?.Auth.CurrentUser != null);
    }

    public Task<AppUser?> GetCurrentUserAsync()
    {
        var user = _supabase?.Auth.CurrentUser;
        return Task.FromResult(user != null ? MapToAppUser(user) : null);
    }

    public async Task<AuthResult> SignInWithGoogleAsync()
    {
        try
        {
            if (_supabase == null)
                return new AuthResult(false, "Service not initialized");

            // Get the OAuth URL from Supabase
            var providerAuth = await _supabase.Auth.SignIn(
                Constants.Provider.Google,
                new SignInOptions
                {
                    RedirectTo = CallbackUrl
                }
            );

            if (providerAuth?.Uri == null)
                return new AuthResult(false, "Failed to get auth URL");

            // Use MAUI WebAuthenticator for the OAuth flow
            var authResult = await WebAuthenticator.Default.AuthenticateAsync(
                new WebAuthenticatorOptions
                {
                    Url = providerAuth.Uri,
                    CallbackUrl = new Uri(CallbackUrl)
                }
            );

            // Extract tokens from callback - Supabase returns them as fragment parameters
            string? accessToken = null;
            string? refreshToken = null;

            if (authResult.Properties.TryGetValue("access_token", out var at))
                accessToken = at;
            if (authResult.Properties.TryGetValue("refresh_token", out var rt))
                refreshToken = rt;

            if (string.IsNullOrEmpty(accessToken))
            {
                // Try to get from the response directly
                accessToken = authResult.AccessToken;
            }

            if (string.IsNullOrEmpty(accessToken))
                return new AuthResult(false, "No access token received");

            // Set the session in Supabase client
            await _supabase.Auth.SetSession(accessToken, refreshToken ?? "");

            // Persist session
            await PersistSessionAsync();

            return new AuthResult(true);
        }
        catch (TaskCanceledException)
        {
            return new AuthResult(false, "Sign-in was cancelled");
        }
        catch (Exception ex)
        {
            return new AuthResult(false, ex.Message);
        }
    }

    public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
    {
        try
        {
            if (_supabase == null)
                return new AuthResult(false, "Service not initialized");

            var session = await _supabase.Auth.SignIn(email, password);

            if (session?.User == null)
                return new AuthResult(false, "Invalid email or password");

            await PersistSessionAsync();
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            if (message.Contains("Invalid login credentials"))
                message = "Invalid email or password";
            return new AuthResult(false, message);
        }
    }

    public async Task<AuthResult> SignUpWithEmailAsync(string email, string password)
    {
        try
        {
            if (_supabase == null)
                return new AuthResult(false, "Service not initialized");

            var session = await _supabase.Auth.SignUp(email, password);

            if (session?.User == null)
                return new AuthResult(false, "Failed to create account");

            await PersistSessionAsync();
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            if (message.Contains("already registered"))
                message = "An account with this email already exists";
            return new AuthResult(false, message);
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            if (_supabase != null)
            {
                await _supabase.Auth.SignOut();
            }

            SecureStorage.Remove(SessionStorageKey);
            SecureStorage.Remove("current_den_id"); // Clear den storage
        }
        catch
        {
            // Best effort sign out
        }
    }

    public async Task<bool> HasDenAsync()
    {
        Console.WriteLine("[AuthService] HasDenAsync called");
        var denService = _serviceProvider.GetRequiredService<IDenService>();
        await denService.InitializeAsync();
        var denId = denService.GetCurrentDenId();
        var hasDen = !string.IsNullOrEmpty(denId);
        Console.WriteLine($"[AuthService] HasDenAsync - result: {hasDen}, denId: {denId ?? "null"}");
        return hasDen;
    }

    public async Task<Den?> GetCurrentDenAsync()
    {
        var denService = _serviceProvider.GetRequiredService<IDenService>();
        await denService.InitializeAsync();
        return await denService.GetCurrentDenAsync();
    }

    public async Task CreateDenAsync(string denName)
    {
        // Delegate to DenService for actual Supabase operations
        var denService = _serviceProvider.GetRequiredService<IDenService>();
        await denService.InitializeAsync();
        await denService.CreateDenAsync(denName);
    }

    public Supabase.Client? GetSupabaseClient() => _supabase;

    private static AppUser? MapToAppUser(User? user)
    {
        if (user == null) return null;

        return new AppUser
        {
            Id = user.Id ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.UserMetadata?.ContainsKey("full_name") == true
                ? user.UserMetadata["full_name"]?.ToString()
                : user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
