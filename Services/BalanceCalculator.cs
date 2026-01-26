namespace Denly.Services;

/// <summary>
/// Pure business logic for expense balance calculations.
/// Extracted from SupabaseExpenseService for testability.
/// </summary>
public static class BalanceCalculator
{
    public readonly record struct ExpenseSplitShare(string MemberId, decimal Percent);

    /// <summary>
    /// Calculates balances for each member based on expenses paid.
    /// Positive balance = member is owed money (paid more than fair share).
    /// Negative balance = member owes money (paid less than fair share).
    /// </summary>
    /// <param name="expenses">List of expense amounts and who paid them</param>
    /// <param name="memberIds">List of member IDs participating in the split</param>
    /// <returns>Dictionary mapping member ID to their balance</returns>
    public static Dictionary<string, decimal> CalculateBalances(
        IEnumerable<(decimal Amount, string PaidBy)> expenses,
        IReadOnlyList<string> memberIds)
    {
        var balances = new Dictionary<string, decimal>();

        if (memberIds.Count < 2)
            return balances;

        var expenseList = expenses.ToList();
        var totalExpenses = expenseList.Sum(e => e.Amount);
        var fairShare = totalExpenses / memberIds.Count;

        foreach (var memberId in memberIds)
        {
            var paid = expenseList
                .Where(e => e.PaidBy == memberId)
                .Sum(e => e.Amount);

            // Positive balance = owed money, negative = owes money
            balances[memberId] = paid - fairShare;
        }

        return balances;
    }

    /// <summary>
    /// Calculates balances with custom split percentages.
    /// </summary>
    /// <param name="expenses">List of expense amounts and who paid them</param>
    /// <param name="memberSplits">Dictionary mapping member ID to their split percentage (0-100)</param>
    /// <returns>Dictionary mapping member ID to their balance</returns>
    public static Dictionary<string, decimal> CalculateBalancesWithSplit(
        IEnumerable<(decimal Amount, string PaidBy)> expenses,
        IReadOnlyDictionary<string, decimal> memberSplits)
    {
        var balances = new Dictionary<string, decimal>();

        if (memberSplits.Count < 2)
            return balances;

        var expenseList = expenses.ToList();
        var totalExpenses = expenseList.Sum(e => e.Amount);

        foreach (var (memberId, splitPercent) in memberSplits)
        {
            var paid = expenseList
                .Where(e => e.PaidBy == memberId)
                .Sum(e => e.Amount);

            var owes = totalExpenses * (splitPercent / 100m);

            // Positive balance = owed money, negative = owes money
            balances[memberId] = paid - owes;
        }

        return balances;
    }

    /// <summary>
    /// Calculates balances with per-expense split percentages.
    /// </summary>
    /// <param name="expenses">List of expense amounts, payer, and expense IDs</param>
    /// <param name="memberIds">List of member IDs participating in the split</param>
    /// <param name="splitsByExpenseId">Splits per expense ID</param>
    /// <returns>Dictionary mapping member ID to their balance</returns>
    public static Dictionary<string, decimal> CalculateBalancesWithExpenseSplits(
        IEnumerable<(string ExpenseId, decimal Amount, string PaidBy)> expenses,
        IReadOnlyList<string> memberIds,
        IReadOnlyDictionary<string, IReadOnlyList<ExpenseSplitShare>> splitsByExpenseId)
    {
        var balances = new Dictionary<string, decimal>();

        if (memberIds.Count < 2)
            return balances;

        foreach (var memberId in memberIds)
        {
            balances[memberId] = 0m;
        }

        foreach (var expense in expenses)
        {
            if (!balances.ContainsKey(expense.PaidBy))
                continue;

            balances[expense.PaidBy] += expense.Amount;

            if (splitsByExpenseId.TryGetValue(expense.ExpenseId, out var splits) && splits.Count > 0)
            {
                var totalPercent = splits.Sum(s => s.Percent);
                if (totalPercent <= 0)
                {
                    var evenShare = expense.Amount / memberIds.Count;
                    foreach (var memberId in memberIds)
                    {
                        balances[memberId] -= evenShare;
                    }
                    continue;
                }

                foreach (var split in splits)
                {
                    if (!balances.ContainsKey(split.MemberId))
                        continue;
                    var normalized = split.Percent / totalPercent;
                    balances[split.MemberId] -= expense.Amount * normalized;
                }
            }
            else
            {
                var evenShare = expense.Amount / memberIds.Count;
                foreach (var memberId in memberIds)
                {
                    balances[memberId] -= evenShare;
                }
            }
        }

        return balances;
    }
}
