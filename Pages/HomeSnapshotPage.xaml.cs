using Denly.Services;
using Denly.ViewModels;

namespace Denly.Pages;

public partial class HomeSnapshotPage : ContentPage
{
    private readonly HomeSnapshotViewModel _viewModel;

    public HomeSnapshotPage()
    {
        InitializeComponent();

        // Resolve DenTimeService for Den time formatting; fallback to local if unavailable.
        var services = Application.Current?.Handler?.MauiContext?.Services;
        var denTimeService = services?.GetService(typeof(IDenTimeService)) as IDenTimeService
            ?? new DenTimeService(new FallbackDenService());

        _viewModel = new HomeSnapshotViewModel(denTimeService);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
    private sealed class FallbackDenService : IDenService
    {
        public Task InitializeAsync() => Task.CompletedTask;
        public Task ResetAsync() => Task.CompletedTask;
        public Task<Denly.Models.Den?> GetCurrentDenAsync() => Task.FromResult<Denly.Models.Den?>(null);
        public Task<List<Denly.Models.Den>> GetUserDensAsync() => Task.FromResult(new List<Denly.Models.Den>());
        public Task SetCurrentDenAsync(string denId) => Task.CompletedTask;
        public Task<Denly.Models.Den> CreateDenAsync(string name) => Task.FromResult(new Denly.Models.Den());
        public string? GetCurrentDenId() => null;
        public Task<List<Denly.Models.DenMember>> GetDenMembersAsync(string? denId = null) => Task.FromResult(new List<Denly.Models.DenMember>());
        public Task RemoveMemberAsync(string denId, string userId) => Task.CompletedTask;
        public Task<bool> IsOwnerAsync(string? denId = null) => Task.FromResult(false);
        public Task<Dictionary<string, Denly.Models.Profile>> GetProfilesAsync(List<string> userIds) => Task.FromResult(new Dictionary<string, Denly.Models.Profile>());
        public Task<Denly.Models.DenInvite> CreateInviteAsync(string? denId = null, string role = "co-parent") => Task.FromResult(new Denly.Models.DenInvite());
        public Task<Denly.Models.DenInvite?> GetActiveInviteAsync(string? denId = null) => Task.FromResult<Denly.Models.DenInvite?>(null);
        public Task DeleteInviteAsync(string inviteId) => Task.CompletedTask;
        public Task<Denly.Models.DenInvite?> ValidateInviteCodeAsync(string code) => Task.FromResult<Denly.Models.DenInvite?>(null);
        public Task<Denly.Services.JoinDenResult> JoinDenAsync(string code) => Task.FromResult(new Denly.Services.JoinDenResult(false));
        public Task<int> GetFailedAttemptsCountAsync(int minutes = 15) => Task.FromResult(0);
        public event EventHandler<Denly.Services.DenChangedEventArgs>? DenChanged;
        public Task<List<Denly.Models.Child>> GetChildrenAsync() => Task.FromResult(new List<Denly.Models.Child>());
        public Task UpdateChildAsync(Denly.Models.Child child) => Task.CompletedTask;
    }
}
