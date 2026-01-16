using Denly.Models;
using Microsoft.Extensions.Logging;

namespace Denly.Services;

public class SupabaseExpenseService : SupabaseServiceBase, IExpenseService, IDisposable
{
    private const string ReceiptsBucket = "receipts";
    private readonly IStorageService _storageService;
    private readonly ILogger<SupabaseExpenseService> _logger;
    private const int CacheTtlMinutes = 5;
    private Dictionary<string, decimal>? _balancesCache;
    private DateTime _balancesCacheTime;
    private string? _balancesCacheDenId;

    public SupabaseExpenseService(IDenService denService, IAuthService authService, IStorageService storageService, ILogger<SupabaseExpenseService> logger)
        : base(denService, authService)
    {
        _storageService = storageService;
        _logger = logger;
        DenService.DenChanged += OnDenChanged;
    }

    private void OnDenChanged(object? sender, DenChangedEventArgs e)
    {
        InvalidateBalanceCache();
    }

    public void InvalidateBalanceCache()
    {
        _balancesCache = null;
        _balancesCacheTime = default;
        _balancesCacheDenId = null;
        _logger.LogDebug("Balances cache invalidated");
    }

    public async Task<List<Expense>> GetAllExpensesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Expense>();

        try
        {
            var response = await SupabaseClient!
                .From<Expense>()
                .Select("id, den_id, child_id, description, amount, paid_by, receipt_url, created_by, created_at, settled_at")
                .Where(e => e.DenId == denId)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            // Populate PaidByName from profiles
            var expenses = response.Models;
            await PopulateExpenseNamesAsync(expenses);

            return expenses
                .OrderByDescending(e => e.CreatedAt)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all expenses");
            return new List<Expense>();
        }
    }

    private async Task PopulateExpenseNamesAsync(List<Expense> expenses)
    {
        var userIds = expenses.Select(e => e.PaidBy).Distinct().ToList();
        if (userIds.Count == 0) return;

        try
        {
            // Use cached profile lookup from DenService
            var profiles = await DenService.GetProfilesAsync(userIds);

            foreach (var expense in expenses)
            {
                if (profiles.TryGetValue(expense.PaidBy, out var profile))
                {
                    expense.PaidByName = profile.DisplayName;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to populate expense names");
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await SupabaseClient!
                .From<Expense>()
                .Select("id, den_id, child_id, description, amount, paid_by, receipt_url, created_by, created_at, settled_at")
                .Where(e => e.Id == id)
                .Single();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expense by ID");
            return null;
        }
    }

    public async Task SaveExpenseAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SaveExpenseAsync called");

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            _logger.LogWarning("Cannot save expense - no den selected");
            return;
        }

        // Get user ID directly from the Supabase auth session
        var supabaseUser = SupabaseClient?.Auth.CurrentUser;
        if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
        {
            _logger.LogWarning("Cannot save expense - no authenticated session");
            return;
        }

        var userId = supabaseUser.Id;

        expense.DenId = denId;

        var existing = await GetExpenseByIdAsync(expense.Id, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (existing != null)
        {
            _logger.LogDebug("Updating existing expense");
            try
            {
                await SupabaseClient!
                    .From<Expense>()
                    .Where(e => e.Id == expense.Id)
                    .Set(e => e.Description, expense.Description)
                    .Set(e => e.Amount, expense.Amount)
                    .Set(e => e.PaidBy, expense.PaidBy)
                    .Set(e => e.ReceiptUrl!, expense.ReceiptUrl)
                    .Set(e => e.ChildId!, expense.ChildId)
                    .Set(e => e.SettledAt!, expense.SettledAt)
                    .Update();
                _logger.LogDebug("Expense updated successfully");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update expense");
            }
        }
        else
        {
            _logger.LogDebug("Inserting new expense");
            expense.CreatedBy = supabaseUser.Id;
            expense.CreatedAt = DateTime.UtcNow;

            try
            {
                await SupabaseClient!
                    .From<Expense>()
                    .Insert(expense);
                _logger.LogDebug("Expense inserted successfully");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert expense");
            }
        }
        InvalidateBalanceCache();
    }

    public async Task DeleteExpenseAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("DeleteExpenseAsync called");
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Get expense to delete receipt if exists
            var expense = await GetExpenseByIdAsync(id, cancellationToken);
            if (expense?.ReceiptUrl != null)
            {
                await DeleteReceiptAsync(expense.ReceiptUrl, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            await SupabaseClient!
                .From<Expense>()
                .Where(e => e.Id == id)
                .Delete();
            _logger.LogDebug("Expense deleted successfully");
            InvalidateBalanceCache();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete expense");
        }
    }

    public async Task<Dictionary<string, decimal>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new Dictionary<string, decimal>();

        if (_balancesCache != null && _balancesCacheDenId == denId && DateTime.UtcNow - _balancesCacheTime < TimeSpan.FromMinutes(CacheTtlMinutes))
        {
            _logger.LogDebug("Returning cached balances");
            return _balancesCache;
        }

        var balances = new Dictionary<string, decimal>();

        try
        {
            // Get unsettled expenses (where settled_at is null)
            var expenseResponse = await SupabaseClient!
                .From<Expense>()
                .Select("id, den_id, child_id, description, amount, paid_by, receipt_url, created_by, created_at, settled_at")
                .Where(e => e.DenId == denId)
                .Filter<DateTime?>("settled_at", Supabase.Postgrest.Constants.Operator.Is, null)
                .Get();

            var unsettledExpenses = expenseResponse.Models;

            // Get den members
            var members = await DenService.GetDenMembersAsync(denId);
            var memberIds = members.Select(m => m.UserId).ToList();

            if (memberIds.Count < 2)
                return balances;

            // Calculate total and what each person paid
            var totalExpenses = unsettledExpenses.Sum(e => e.Amount);
            var fairShare = totalExpenses / memberIds.Count;

            foreach (var memberId in memberIds)
            {
                var paid = unsettledExpenses.Where(e => e.PaidBy == memberId).Sum(e => e.Amount);
                // Positive balance = owed money, negative = owes money
                balances[memberId] = paid - fairShare;
            }

            _balancesCache = balances;
            _balancesCacheDenId = denId;
            _balancesCacheTime = DateTime.UtcNow;
            _logger.LogDebug("Balances fetched and cached");

            return balances;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balances");
            return balances;
        }
    }

    public async Task<List<Settlement>> GetAllSettlementsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId)) return new List<Settlement>();

        try
        {
            var response = await SupabaseClient!
                .From<Settlement>()
                .Select("id, den_id, from_user_id, to_user_id, amount, note, created_by, created_at")
                .Where(s => s.DenId == denId)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var settlements = response.Models;
            await PopulateSettlementNamesAsync(settlements);

            return settlements;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settlements");
            return new List<Settlement>();
        }
    }

    private async Task PopulateSettlementNamesAsync(List<Settlement> settlements)
    {
        var userIds = settlements
            .SelectMany(s => new[] { s.FromUserId, s.ToUserId })
            .Distinct()
            .ToList();

        if (userIds.Count == 0) return;

        try
        {
            // Use cached profile lookup from DenService
            var profiles = await DenService.GetProfilesAsync(userIds);

            foreach (var settlement in settlements)
            {
                if (profiles.TryGetValue(settlement.FromUserId, out var fromProfile))
                {
                    settlement.FromUserName = fromProfile.DisplayName;
                }
                if (profiles.TryGetValue(settlement.ToUserId, out var toProfile))
                {
                    settlement.ToUserName = toProfile.DisplayName;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to populate settlement names");
        }
    }

    public async Task<Settlement> CreateSettlementAsync(decimal amount, string fromUserId, string toUserId, string? note = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("CreateSettlementAsync called - Amount: {Amount}", amount);

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            _logger.LogWarning("Cannot create settlement - no den selected");
            throw new InvalidOperationException("No den selected");
        }

        var supabaseUser = SupabaseClient?.Auth.CurrentUser;
        if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
        {
            _logger.LogWarning("Cannot create settlement - no authenticated session");
            throw new InvalidOperationException("User not authenticated");
        }

        var userId = supabaseUser.Id;

        var settlement = new Settlement
        {
            DenId = denId,
            Amount = amount,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Note = note,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogDebug("Inserting settlement");
            cancellationToken.ThrowIfCancellationRequested();

            await SupabaseClient!
                .From<Settlement>()
                .Insert(settlement);
            _logger.LogDebug("Settlement inserted successfully");

            // Mark all unsettled expenses as settled
            var unsettled = await SupabaseClient!
                .From<Expense>()
                .Select("id, den_id, child_id, description, amount, paid_by, receipt_url, created_by, created_at, settled_at")
                .Where(e => e.DenId == denId)
                .Filter<DateTime?>("settled_at", Supabase.Postgrest.Constants.Operator.Is, null)
                .Get();

            var settledAt = DateTime.UtcNow;
            foreach (var expense in unsettled.Models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SupabaseClient!
                    .From<Expense>()
                    .Where(e => e.Id == expense.Id)
                    .Set(e => e.SettledAt!, settledAt)
                    .Update();
            }
            _logger.LogDebug("Marked {Count} expenses as settled", unsettled.Models.Count);
            InvalidateBalanceCache();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create settlement");
            throw;
        }

        return settlement;
    }

    public async Task<string> SaveReceiptAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SaveReceiptAsync called");

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            _logger.LogWarning("Cannot save receipt - no den selected");
            throw new InvalidOperationException("No den selected");
        }

        try
        {
            var url = await _storageService.UploadAsync(ReceiptsBucket, imageStream, fileName, denId, cancellationToken);
            _logger.LogDebug("Receipt uploaded successfully");
            return url;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload receipt");
            throw;
        }
    }

    public async Task DeleteReceiptAsync(string receiptUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(receiptUrl)) return;

        _logger.LogDebug("Deleting receipt");
        await _storageService.DeleteAsync(ReceiptsBucket, receiptUrl, cancellationToken);
        _logger.LogDebug("Receipt deleted successfully");
    }

    public async Task<bool> HasExpensesAsync()
    {
        await EnsureInitializedAsync();
        var denId = DenService.GetCurrentDenId();
        if (denId == null) return false;

        try
        {
            var result = await SupabaseClient!
                .From<Expense>()
                .Select("id")
                .Filter("den_id", Supabase.Postgrest.Constants.Operator.Equals, denId)
                .Limit(1)
                .Get();

            return result.Models.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for expenses");
            return false;
        }
    }

    public void Dispose()
    {
        DenService.DenChanged -= OnDenChanged;
    }
}
