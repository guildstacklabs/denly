using Denly.Models;

namespace Denly.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetAllExpensesAsync();
    Task<Expense?> GetExpenseByIdAsync(string id);
    Task SaveExpenseAsync(Expense expense);
    Task DeleteExpenseAsync(string id);
    Task<Dictionary<string, decimal>> GetBalancesAsync();
    Task<List<Settlement>> GetAllSettlementsAsync();
    Task<Settlement> CreateSettlementAsync(decimal amount, string fromUserId, string toUserId, string? note = null);
    Task<string> SaveReceiptAsync(Stream imageStream, string fileName);
    Task DeleteReceiptAsync(string receiptPath);
}
