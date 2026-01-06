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
}

public class LocalExpenseService : IExpenseService
{
    private readonly string _filePath;
    private List<Expense>? _cache;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LocalExpenseService()
    {
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "expenses.json");
    }

    private async Task<List<Expense>> LoadExpensesAsync()
    {
        if (_cache != null)
            return _cache;

        if (!File.Exists(_filePath))
        {
            _cache = new List<Expense>();
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _cache = JsonSerializer.Deserialize<List<Expense>>(json, _jsonOptions) ?? new List<Expense>();
        }
        catch
        {
            _cache = new List<Expense>();
        }

        return _cache;
    }

    private async Task SaveExpensesAsync(List<Expense> expenses)
    {
        var json = JsonSerializer.Serialize(expenses, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
        _cache = expenses;
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

        // Calculate how much each parent has paid
        var parentAPaid = expenses.Where(e => e.PaidBy == Parent.ParentA).Sum(e => e.Amount);
        var parentBPaid = expenses.Where(e => e.PaidBy == Parent.ParentB).Sum(e => e.Amount);

        // With 50/50 split, each parent should pay half of total
        var total = parentAPaid + parentBPaid;
        var fairShare = total / 2;

        // Positive = Parent A owes Parent B
        // Negative = Parent B owes Parent A
        return fairShare - parentAPaid;
    }
}
