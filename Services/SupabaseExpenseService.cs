using Denly.Models;
namespace Denly.Services;

public class SupabaseExpenseService : SupabaseServiceBase, IExpenseService
{
    private const string ReceiptsBucket = "receipts";

    public SupabaseExpenseService(IDenService denService, IAuthService authService)
        : base(denService, authService)
    {
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        await EnsureInitializedAsync();

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpenseService] Error getting all expenses");
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
            _logger.LogError(ex, "[ExpenseService] Error populating expense names");
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
                _logger.LogInformation("[ExpenseService] Expense updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExpenseService] Error updating expense");
            }
        }
        else
        {
            _logger.LogInformation("[ExpenseService] Inserting new expense");
            expense.CreatedBy = supabaseUser.Id;
            expense.CreatedAt = DateTime.UtcNow;

            try
            {
                var response = await SupabaseClient!
                    .From<Expense>()
                    .Insert(expense);
                _logger.LogInformation("[ExpenseService] Expense insert response: {Count} models returned", response?.Models?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExpenseService] Error inserting expense");
            }
        }
    }

    public async Task DeleteExpenseAsync(string id)
    {
        _logger.LogInformation("[ExpenseService] DeleteExpenseAsync called");
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
            _logger.LogInformation("[ExpenseService] Expense deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpenseService] Error deleting expense");
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

            return balances;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpenseService] Error getting balances");
            return balances;
        }
    }

    public async Task<List<Settlement>> GetAllSettlementsAsync()
    {
        await EnsureInitializedAsync();

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpenseService] Error getting settlements");
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
            _logger.LogInformation("[ExpenseService] Marked {Count} expenses as settled", unsettled.Models.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpenseService] Error creating settlement");
            throw;
        }

        return settlement;
    }

    public async Task<string> SaveReceiptAsync(Stream imageStream, string fileName)
    {
        _logger.LogInformation("[ExpenseService] SaveReceiptAsync called");

        await EnsureInitializedAsync();

        var denId = _denService.GetCurrentDenId();
        if (string.IsNullOrEmpty(denId))
        {
            _logger.LogWarning("[ExpenseService] No den selected");
            throw new InvalidOperationException("No den selected");
        }

        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{denId}/{Guid.NewGuid()}{extension}";

        try
        {
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            _logger.LogInformation("[ExpenseService] Uploading receipt");
            await SupabaseClient!.Storage
                .From(ReceiptsBucket)
                .Upload(bytes, uniqueName);

            // Return the public URL
            var url = SupabaseClient!.Storage
                .From(ReceiptsBucket)
                .GetPublicUrl(uniqueName);

            _logger.LogInformation("[ExpenseService] Receipt uploaded successfully");
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpenseService] Error uploading receipt");
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

            _logger.LogInformation("[ExpenseService] Deleting receipt");
            await SupabaseClient!.Storage
                .From(ReceiptsBucket)
                .Remove(new List<string> { path });
            _logger.LogInformation("[ExpenseService] Receipt deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpenseService] Error deleting receipt");
        }
    }
}
