using Denly.Models;

namespace Denly.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(CancellationToken cancellationToken = default);
    Task<Expense?> GetExpenseByIdAsync(string id, CancellationToken cancellationToken = default);
    Task SaveExpenseAsync(Expense expense, CancellationToken cancellationToken = default);
    Task DeleteExpenseAsync(string id, CancellationToken cancellationToken = default);
    Task<Dictionary<string, decimal>> GetBalancesAsync(CancellationToken cancellationToken = default);
    Task<List<Settlement>> GetAllSettlementsAsync(CancellationToken cancellationToken = default);
    Task<Settlement> CreateSettlementAsync(decimal amount, string fromUserId, string toUserId, string? note = null, CancellationToken cancellationToken = default);
    Task<string> SaveReceiptAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);
    Task DeleteReceiptAsync(string receiptPath, CancellationToken cancellationToken = default);
    Task<bool> HasExpensesAsync();
}
