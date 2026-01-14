using Denly.Models;
using Supabase;

namespace Denly.Services;

public class SupabaseExpenseService : IExpenseService
{
    private const string ReceiptsBucket = "receipts";

    private readonly IDenService _denService;
    private readonly IAuthService _authService;
    private bool _isInitialized;

    // Use the authenticated client from AuthService
    private Supabase.Client? SupabaseClient => _authService.GetSupabaseClient();

    public SupabaseExpenseService(IDenService denService, IAuthService authService)
    {
        _denService = denService;
        _authService = authService;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        // Ensure auth service is initialized (which creates the authenticated client)
        await _authService.InitializeAsync();
        // Ensure den service is initialized (to restore current den from storage)
        await _denService.InitializeAsync();
        _isInitialized = true;
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
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
            var profiles = await SupabaseClient!
                .From<Profile>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, userIds)
                .Get();

            var profileDict = profiles.Models.ToDictionary(p => p.Id, p => p.DisplayName);

            foreach (var expense in expenses)
            {
                if (profileDict.TryGetValue(expense.PaidBy, out var name))
                {
                    expense.PaidByName = name;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error populating expense names: {ex.Message}");
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(string id)
    {
        await EnsureInitializedAsync();

        try
        {
            return await SupabaseClient!
                .From<Expense>()
                .Where(e => e.Id == id)
                .Single();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error getting expense by id: {ex.Message}");
            return null;
        }
    }

    public async Task SaveExpenseAsync(Expense expense)
    {
        Console.WriteLine($"[ExpenseService] SaveExpenseAsync called for: {expense.Description}");

        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
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

        var existing = await GetExpenseByIdAsync(expense.Id);

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
                Console.WriteLine($"[ExpenseService] Expense updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExpenseService] Error updating expense: {ex.Message}");
                Console.WriteLine($"[ExpenseService] Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            Console.WriteLine($"[ExpenseService] Inserting new expense: {expense.Id}");
            expense.CreatedBy = userId;
            expense.CreatedAt = DateTime.UtcNow;

            Console.WriteLine($"[ExpenseService] Expense object - Id: {expense.Id}, DenId: {expense.DenId}, CreatedBy: {expense.CreatedBy}, Amount: {expense.Amount}, PaidBy: {expense.PaidBy}");

            try
            {
                var response = await SupabaseClient!
                    .From<Expense>()
                    .Insert(expense);
                Console.WriteLine($"[ExpenseService] Expense insert response: {response?.Models?.Count ?? 0} models returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExpenseService] Error inserting expense: {ex.Message}");
                Console.WriteLine($"[ExpenseService] Stack trace: {ex.StackTrace}");
            }
        }
    }

    public async Task DeleteExpenseAsync(string id)
    {
        Console.WriteLine($"[ExpenseService] DeleteExpenseAsync called for: {id}");
        await EnsureInitializedAsync();

        try
        {
            // Get expense to delete receipt if exists
            var expense = await GetExpenseByIdAsync(id);
            if (expense?.ReceiptUrl != null)
            {
                await DeleteReceiptAsync(expense.ReceiptUrl);
            }

            await SupabaseClient!
                .From<Expense>()
                .Where(e => e.Id == id)
                .Delete();
            Console.WriteLine($"[ExpenseService] Expense deleted successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error deleting expense: {ex.Message}");
            Console.WriteLine($"[ExpenseService] Stack trace: {ex.StackTrace}");
        }
    }

    public async Task<Dictionary<string, decimal>> GetBalancesAsync()
    {
        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
        var balances = new Dictionary<string, decimal>();

        if (string.IsNullOrEmpty(denId)) return balances;

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
            var members = await _denService.GetDenMembersAsync(denId);
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

            return balances;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error getting balances: {ex.Message}");
            return balances;
        }
    }

    public async Task<List<Settlement>> GetAllSettlementsAsync()
    {
        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
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
            var profiles = await SupabaseClient!
                .From<Profile>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, userIds)
                .Get();

            var profileDict = profiles.Models.ToDictionary(p => p.Id, p => p.DisplayName);

            foreach (var settlement in settlements)
            {
                if (profileDict.TryGetValue(settlement.FromUserId, out var fromName))
                {
                    settlement.FromUserName = fromName;
                }
                if (profileDict.TryGetValue(settlement.ToUserId, out var toName))
                {
                    settlement.ToUserName = toName;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error populating settlement names: {ex.Message}");
        }
    }

    public async Task<Settlement> CreateSettlementAsync(decimal amount, string fromUserId, string toUserId, string? note = null)
    {
        Console.WriteLine($"[ExpenseService] CreateSettlementAsync called - Amount: {amount}, From: {fromUserId}, To: {toUserId}");

        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
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
                await SupabaseClient!
                    .From<Expense>()
                    .Where(e => e.Id == expense.Id)
                    .Set(e => e.SettledAt!, settledAt)
                    .Update();
            }
            Console.WriteLine($"[ExpenseService] Marked {unsettled.Models.Count} expenses as settled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error creating settlement: {ex.Message}");
            Console.WriteLine($"[ExpenseService] Stack trace: {ex.StackTrace}");
            throw;
        }

        return settlement;
    }

    public async Task<string> SaveReceiptAsync(Stream imageStream, string fileName)
    {
        Console.WriteLine($"[ExpenseService] SaveReceiptAsync called for: {fileName}");

        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            Console.WriteLine("[ExpenseService] Error: No den selected");
            throw new InvalidOperationException("No den selected");
        }

        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{denId}/{Guid.NewGuid()}{extension}";

        try
        {
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            Console.WriteLine($"[ExpenseService] Uploading receipt to: {uniqueName}");
            await SupabaseClient!.Storage
                .From(ReceiptsBucket)
                .Upload(bytes, uniqueName);

            // Return the public URL
            var url = SupabaseClient!.Storage
                .From(ReceiptsBucket)
                .GetPublicUrl(uniqueName);

            Console.WriteLine($"[ExpenseService] Receipt uploaded successfully: {url}");
            return url;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error uploading receipt: {ex.Message}");
            Console.WriteLine($"[ExpenseService] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task DeleteReceiptAsync(string receiptUrl)
    {
        if (string.IsNullOrEmpty(receiptUrl)) return;

        await EnsureInitializedAsync();

        try
        {
            // Extract path from URL
            var uri = new Uri(receiptUrl);
            var path = uri.AbsolutePath.Replace($"/storage/v1/object/public/{ReceiptsBucket}/", "");

            Console.WriteLine($"[ExpenseService] Deleting receipt: {path}");
            await SupabaseClient!.Storage
                .From(ReceiptsBucket)
                .Remove(new List<string> { path });
            Console.WriteLine($"[ExpenseService] Receipt deleted successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseService] Error deleting receipt: {ex.Message}");
        }
    }
}
