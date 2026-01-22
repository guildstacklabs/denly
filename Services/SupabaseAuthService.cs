using System.Text.Json;
using Denly.Models;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Denly.Services;

public class SupabaseAuthService : IAuthService
{
    private const string CallbackUrl = "com.companyname.denly://login-callback";
    private const string SessionStorageKey = "supabase_session";

    private const string MissingConfigMessage = "Supabase configuration is missing.";

    private readonly IServiceProvider _serviceProvider;
    private readonly DenlyOptions _options;
    private readonly ILogger<SupabaseAuthService> _logger;
    private Supabase.Client? _supabase;
    private bool _isInitialized;
    private string? _initializationError;

    public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

    public SupabaseAuthService(IServiceProvider serviceProvider, IOptions<DenlyOptions> options, ILogger<SupabaseAuthService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        System.Diagnostics.Debug.WriteLine(">>> AuthService: InitializeAsync starting...");

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        if (string.IsNullOrWhiteSpace(_options.SupabaseUrl) || string.IsNullOrWhiteSpace(_options.SupabaseAnonKey))
        {
            _initializationError = MissingConfigMessage;
            System.Diagnostics.Debug.WriteLine(">>> AuthService: Missing Supabase config - URL or Key is empty");
            _logger.LogWarning("Supabase configuration is missing. URL={Url}, KeyPresent={KeyPresent}",
                _options.SupabaseUrl ?? "(null)",
                !string.IsNullOrWhiteSpace(_options.SupabaseAnonKey));
            return;
        }

        System.Diagnostics.Debug.WriteLine($">>> AuthService: Config OK, URL={_options.SupabaseUrl}");

        try
        {
            _supabase = new Supabase.Client(_options.SupabaseUrl, _options.SupabaseAnonKey, options);

            System.Diagnostics.Debug.WriteLine(">>> AuthService: Calling _supabase.InitializeAsync()...");

            // Add timeout to prevent indefinite hanging
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var initTask = _supabase.InitializeAsync();
            var completedTask = await Task.WhenAny(initTask, Task.Delay(Timeout.Infinite, cts.Token));

            if (completedTask != initTask)
            {
                throw new TimeoutException("Supabase initialization timed out after 10 seconds");
            }

            await initTask; // Propagate any exception

            System.Diagnostics.Debug.WriteLine(">>> AuthService: Supabase initialized, restoring session...");

            // Try to restore session from secure storage
            await RestoreSessionAsync();

            System.Diagnostics.Debug.WriteLine(">>> AuthService: Session restored, adding state listener...");

            // Listen for auth state changes
            _supabase.Auth.AddStateChangedListener((sender, state) =>
            {
                var isAuth = state == Constants.AuthState.SignedIn;
                var user = isAuth ? MapToAppUser(_supabase.Auth.CurrentUser) : null;
                AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(isAuth, user));
            });

            _initializationError = null;
            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine(">>> AuthService: InitializeAsync complete!");
        }
        catch (Exception ex)
        {
            _initializationError = $"Failed to initialize: {ex.Message}";
            _isInitialized = false; // Allow retries after transient failures.
            System.Diagnostics.Debug.WriteLine($">>> AuthService: EXCEPTION: {ex}");
            _logger.LogError(ex, "Failed to initialize Supabase client");
        }
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
        if (!string.IsNullOrEmpty(_initializationError))
        {
            _logger.LogWarning("IsAuthenticatedAsync called but service has initialization error");
            return Task.FromResult(false);
        }

        return Task.FromResult(_supabase?.Auth.CurrentUser != null);
    }

    public Task<AppUser?> GetCurrentUserAsync()
    {
        if (!string.IsNullOrEmpty(_initializationError))
        {
            _logger.LogWarning("GetCurrentUserAsync called but service has initialization error");
            return Task.FromResult<AppUser?>(null);
        }

        var user = _supabase?.Auth.CurrentUser;
        return Task.FromResult(user != null ? MapToAppUser(user) : null);
    }

    public async Task<AuthResult> SignInWithGoogleAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_initializationError))
                return new AuthResult(false, _initializationError);
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
            if (!string.IsNullOrEmpty(_initializationError))
                return new AuthResult(false, _initializationError);
            if (_supabase == null)
                return new AuthResult(false, "Service not initialized");

            var session = await _supabase.Auth.SignIn(email, password);

            if (session?.User == null)
                return new AuthResult(false, "Invalid email or password");

            if (!string.IsNullOrEmpty(session.AccessToken))
            {
                await _supabase.Auth.SetSession(session.AccessToken, session.RefreshToken ?? string.Empty);
            }

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
            if (!string.IsNullOrEmpty(_initializationError))
                return new AuthResult(false, _initializationError);
            if (_supabase == null)
                return new AuthResult(false, "Service not initialized");

            _logger.LogDebug("SignUpWithEmailAsync starting");
            var session = await _supabase.Auth.SignUp(email, password);

            if (session?.User == null)
            {
                _logger.LogWarning("SignUpWithEmailAsync - no user returned");
                return new AuthResult(false, "Failed to create account");
            }

            _logger.LogInformation("User account created successfully");

            // Check if we have a valid session (email confirmation may be required)
            if (string.IsNullOrEmpty(session.AccessToken))
            {
                _logger.LogDebug("No access token returned - email confirmation may be required");
                return new AuthResult(false, "Please check your email to confirm your account before signing in.");
            }

            await PersistSessionAsync();
            _logger.LogDebug("Signup complete, session persisted");
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign up with email");
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
            _logger.LogDebug("SignOutAsync starting");

            // Reset DenService state first
            var denService = _serviceProvider.GetRequiredService<IDenService>();
            await denService.ResetAsync();

            if (_supabase != null)
            {
                await _supabase.Auth.SignOut();
            }

            SecureStorage.Remove(SessionStorageKey);
            _logger.LogDebug("Sign out complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign out");
            // Best effort sign out
        }
    }

    public async Task<bool> HasDenAsync()
    {
        if (!string.IsNullOrEmpty(_initializationError))
        {
            _logger.LogWarning("HasDenAsync called but service has initialization error");
            return false;
        }

        var denService = _serviceProvider.GetRequiredService<IDenService>();
        await denService.InitializeAsync();
        var denId = denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            await denService.ResetAsync();
            await denService.InitializeAsync();
            denId = denService.GetCurrentDenId();
        }
        var hasDen = !string.IsNullOrEmpty(denId);
        _logger.LogDebug("HasDenAsync result: {HasDen}", hasDen);
        return hasDen;
    }

    public async Task<Den?> GetCurrentDenAsync()
    {
        if (!string.IsNullOrEmpty(_initializationError))
        {
            _logger.LogWarning("GetCurrentDenAsync called but service has initialization error");
            return null;
        }

        var denService = _serviceProvider.GetRequiredService<IDenService>();
        await denService.InitializeAsync();
        return await denService.GetCurrentDenAsync();
    }

    public async Task CreateDenAsync(string denName)
    {
        if (!string.IsNullOrEmpty(_initializationError))
        {
            _logger.LogWarning("CreateDenAsync called but service has initialization error");
            return;
        }

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
