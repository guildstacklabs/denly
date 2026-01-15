using Denly.Models;
namespace Denly.Services;

public class SupabaseExpenseService : SupabaseServiceBase, IExpenseService, IDisposable
{
    private const string ReceiptsBucket = "receipts";
    private readonly IStorageService _storageService;
    private const int CacheTtlMinutes = 5;
    private Dictionary<string, decimal>? _balancesCache;
    private DateTime _balancesCacheTime;
    private string? _balancesCacheDenId;

    public SupabaseExpenseService(IDenService denService, IAuthService authService, IStorageService storageService)
        : base(denService, authService)
    {
        _storageService = storageService;
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
        Console.WriteLine("[ExpenseService] Balances cache invalidated.");
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
            Console.WriteLine($"[ExpenseService] Error getting all expenses: {ex.Message}");
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
            Console.WriteLine($"[ExpenseService] Error populating expense names: {ex.Message}");
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
                .Where(e => e.Id == id)
                .Single();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error getting expense by id: {ex.Message}");
            return null;
        }
    }

    public async Task SaveExpenseAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[ExpenseService] SaveExpenseAsync called for: {expense.Description}");

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            Console.WriteLine("[ExpenseService] Error: No den selected");
            return;
        }
        Console.WriteLine($"[ExpenseService] Den ID: {denId}");

        // Get user ID directly from the Supabase auth session
        var supabaseUser = SupabaseClient?.Auth.CurrentUser;
        if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
        {
            Console.WriteLine("[ExpenseService] Error: No authenticated Supabase session");
            return;
        }

        var userId = supabaseUser.Id;
        Console.WriteLine($"[ExpenseService] Supabase auth.uid(): {userId}");
        Console.WriteLine($"[ExpenseService] Session access token present: {!string.IsNullOrEmpty(SupabaseClient?.Auth.CurrentSession?.AccessToken)}");

        expense.DenId = denId;

        var existing = await GetExpenseByIdAsync(expense.Id, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (existing != null)
        {
            Console.WriteLine($"[ExpenseService] Updating existing expense: {expense.Id}");
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
                Console.WriteLine("[ExpenseService] Expense updated successfully");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExpenseService] Error updating expense: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[ExpenseService] Inserting new expense");
            expense.CreatedBy = supabaseUser.Id;
            expense.CreatedAt = DateTime.UtcNow;

            try
            {
                var response = await SupabaseClient!
                    .From<Expense>()
                    .Insert(expense);
                Console.WriteLine($"[ExpenseService] Expense insert response: {response?.Models?.Count ?? 0} models returned");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExpenseService] Error inserting expense: {ex.Message}");
            }
        }
        InvalidateBalanceCache();
    }

    public async Task DeleteExpenseAsync(string id, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[ExpenseService] DeleteExpenseAsync called");
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
            Console.WriteLine("[ExpenseService] Expense deleted successfully");
            InvalidateBalanceCache();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error deleting expense: {ex.Message}");
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
            Console.WriteLine("[ExpenseService] Returning cached balances.");
            return _balancesCache;
        }

        var balances = new Dictionary<string, decimal>();

        try
        {
            // Get unsettled expenses (where settled_at is null)
            var expenseResponse = await SupabaseClient!
                .From<Expense>()
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
            Console.WriteLine("[ExpenseService] Balances fetched and cached.");

            return balances;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error getting balances: {ex.Message}");
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
            Console.WriteLine($"[ExpenseService] Error getting settlements: {ex.Message}");
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
            Console.WriteLine($"[ExpenseService] Error populating settlement names: {ex.Message}");
        }
    }

    public async Task<Settlement> CreateSettlementAsync(decimal amount, string fromUserId, string toUserId, string? note = null, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[ExpenseService] CreateSettlementAsync called - Amount: {amount}, From: {fromUserId}, To: {toUserId}");

        await EnsureInitializedAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            Console.WriteLine("[ExpenseService] Error: No den selected");
            throw new InvalidOperationException("No den selected");
        }

        var supabaseUser = SupabaseClient?.Auth.CurrentUser;
        if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
        {
            Console.WriteLine("[ExpenseService] Error: No authenticated Supabase session");
            throw new InvalidOperationException("User not authenticated");
        }

        var userId = supabaseUser.Id;
        Console.WriteLine($"[ExpenseService] Supabase auth.uid(): {userId}");

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
            Console.WriteLine($"[ExpenseService] Inserting settlement...");
            cancellationToken.ThrowIfCancellationRequested();

            await SupabaseClient!
                .From<Settlement>()
                .Insert(settlement);
            Console.WriteLine($"[ExpenseService] Settlement inserted successfully");

            // Mark all unsettled expenses as settled
            var unsettled = await SupabaseClient!
                .From<Expense>()
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
            Console.WriteLine($"[ExpenseService] Marked {unsettled.Models.Count} expenses as settled");
            InvalidateBalanceCache();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error creating settlement: {ex.Message}");
            throw;
        }

        return settlement;
    }

    public async Task<string> SaveReceiptAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[ExpenseService] SaveReceiptAsync called");

        var denId = DenService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            Console.WriteLine("[ExpenseService] Warning: No den selected");
            throw new InvalidOperationException("No den selected");
        }

        try
        {
            var url = await _storageService.UploadAsync(ReceiptsBucket, imageStream, fileName, denId, cancellationToken);
            Console.WriteLine("[ExpenseService] Receipt uploaded successfully");
            return url;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error uploading receipt: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteReceiptAsync(string receiptUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(receiptUrl)) return;

        Console.WriteLine("[ExpenseService] Deleting receipt");
        await _storageService.DeleteAsync(ReceiptsBucket, receiptUrl, cancellationToken);
        Console.WriteLine("[ExpenseService] Receipt deleted successfully");
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
            Console.WriteLine($"[ExpenseService] Error checking for expenses: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        DenService.DenChanged -= OnDenChanged;
    }
}
