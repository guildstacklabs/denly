using System.Text.Json;
using Denly.Models;

namespace Denly.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetAllExpensesAsync();
    Task<Expense?> GetExpenseByIdAsync(string id);
    Task SaveExpenseAsync(Expense expense);
    Task DeleteExpenseAsync(string id);
    Task<decimal> GetBalanceAsync();
    Task<List<Settlement>> GetAllSettlementsAsync();
    Task<Settlement> CreateSettlementAsync(decimal amount, Parent fromParent, Parent toParent);
    Task<string> SaveReceiptAsync(Stream imageStream, string fileName);
    Task DeleteReceiptAsync(string receiptPath);
}

public class LocalExpenseService : IExpenseService
{
    private readonly string _expensesFilePath;
    private readonly string _settlementsFilePath;
    private readonly string _receiptsDirectory;
    private List<Expense>? _expensesCache;
    private List<Settlement>? _settlementsCache;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LocalExpenseService()
    {
        _expensesFilePath = Path.Combine(FileSystem.AppDataDirectory, "expenses.json");
        _settlementsFilePath = Path.Combine(FileSystem.AppDataDirectory, "settlements.json");
        _receiptsDirectory = Path.Combine(FileSystem.AppDataDirectory, "receipts");
        Directory.CreateDirectory(_receiptsDirectory);
    }

    private async Task<List<Expense>> LoadExpensesAsync()
    {
        if (_expensesCache != null)
            return _expensesCache;

        if (!File.Exists(_expensesFilePath))
        {
            _expensesCache = new List<Expense>();
            return _expensesCache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_expensesFilePath);
            _expensesCache = JsonSerializer.Deserialize<List<Expense>>(json, _jsonOptions) ?? new List<Expense>();
        }
        catch
        {
            _expensesCache = new List<Expense>();
        }

        return _expensesCache;
    }

    private async Task SaveExpensesAsync(List<Expense> expenses)
    {
        var json = JsonSerializer.Serialize(expenses, _jsonOptions);
        await File.WriteAllTextAsync(_expensesFilePath, json);
        _expensesCache = expenses;
    }

    private async Task<List<Settlement>> LoadSettlementsAsync()
    {
        if (_settlementsCache != null)
            return _settlementsCache;

        if (!File.Exists(_settlementsFilePath))
        {
            _settlementsCache = new List<Settlement>();
            return _settlementsCache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settlementsFilePath);
            _settlementsCache = JsonSerializer.Deserialize<List<Settlement>>(json, _jsonOptions) ?? new List<Settlement>();
        }
        catch
        {
            _settlementsCache = new List<Settlement>();
        }

        return _settlementsCache;
    }

    private async Task SaveSettlementsAsync(List<Settlement> settlements)
    {
        var json = JsonSerializer.Serialize(settlements, _jsonOptions);
        await File.WriteAllTextAsync(_settlementsFilePath, json);
        _settlementsCache = settlements;
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        var expenses = await LoadExpensesAsync();
        return expenses.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToList();
    }

    public async Task<Expense?> GetExpenseByIdAsync(string id)
    {
        var expenses = await LoadExpensesAsync();
        return expenses.FirstOrDefault(e => e.Id == id);
    }

    public async Task SaveExpenseAsync(Expense expense)
    {
        var expenses = await LoadExpensesAsync();
        var existing = expenses.FirstOrDefault(e => e.Id == expense.Id);

        if (existing != null)
        {
            expenses.Remove(existing);
            expense.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            expense.CreatedAt = DateTime.UtcNow;
            expense.UpdatedAt = DateTime.UtcNow;
        }

        expenses.Add(expense);
        await SaveExpensesAsync(expenses);
    }

    public async Task DeleteExpenseAsync(string id)
    {
        var expenses = await LoadExpensesAsync();
        var expense = expenses.FirstOrDefault(e => e.Id == id);

        if (expense != null)
        {
            expenses.Remove(expense);
            await SaveExpensesAsync(expenses);
        }
    }

    public async Task<decimal> GetBalanceAsync()
    {
        var expenses = await LoadExpensesAsync();

        // Only consider unsettled expenses for balance calculation
        var unsettledExpenses = expenses.Where(e => e.SettlementId == null).ToList();

        // Calculate how much each parent has paid
        var parentAPaid = unsettledExpenses.Where(e => e.PaidBy == Parent.ParentA).Sum(e => e.Amount);
        var parentBPaid = unsettledExpenses.Where(e => e.PaidBy == Parent.ParentB).Sum(e => e.Amount);

        // With 50/50 split, each parent should pay half of total
        var total = parentAPaid + parentBPaid;
        var fairShare = total / 2;

        // Positive = Parent A owes Parent B
        // Negative = Parent B owes Parent A
        return fairShare - parentAPaid;
    }

    public async Task<List<Settlement>> GetAllSettlementsAsync()
    {
        var settlements = await LoadSettlementsAsync();
        return settlements.OrderByDescending(s => s.Date).ThenByDescending(s => s.CreatedAt).ToList();
    }

    public async Task<Settlement> CreateSettlementAsync(decimal amount, Parent fromParent, Parent toParent)
    {
        var settlement = new Settlement
        {
            Amount = amount,
            Date = DateTime.Today,
            FromParent = fromParent,
            ToParent = toParent,
            CreatedAt = DateTime.UtcNow
        };

        // Mark all unsettled expenses with this settlement ID
        var expenses = await LoadExpensesAsync();
        foreach (var expense in expenses.Where(e => e.SettlementId == null))
        {
            expense.SettlementId = settlement.Id;
            expense.UpdatedAt = DateTime.UtcNow;
        }
        await SaveExpensesAsync(expenses);

        // Save the settlement
        var settlements = await LoadSettlementsAsync();
        settlements.Add(settlement);
        await SaveSettlementsAsync(settlements);

        return settlement;
    }

    public async Task<string> SaveReceiptAsync(Stream imageStream, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_receiptsDirectory, uniqueName);

        using var fileStream = File.Create(filePath);
        await imageStream.CopyToAsync(fileStream);

        return filePath;
    }

    public async Task DeleteReceiptAsync(string receiptPath)
    {
        if (!string.IsNullOrEmpty(receiptPath) && File.Exists(receiptPath))
        {
            await Task.Run(() => File.Delete(receiptPath));
        }
    }
}
