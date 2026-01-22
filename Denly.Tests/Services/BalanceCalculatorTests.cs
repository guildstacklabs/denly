using Denly.Services;

namespace Denly.Tests.Services;

/// <summary>
/// Tests for expense balance calculations.
/// These tests verify the core business logic for splitting expenses between co-parents.
/// </summary>
public class BalanceCalculatorTests
{
    #region Equal Split (50/50) Tests

    [Fact]
    public void CalculateBalances_TwoMembers_EqualExpenses_ReturnsZeroBalances()
    {
        // Arrange
        var memberIds = new List<string> { "user-a", "user-b" };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (100m, "user-a"),
            (100m, "user-b")
        };

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        Assert.Equal(0m, balances["user-a"]);
        Assert.Equal(0m, balances["user-b"]);
    }

    [Fact]
    public void CalculateBalances_TwoMembers_OnePayerOnly_ReturnsCorrectBalances()
    {
        // Arrange
        var memberIds = new List<string> { "user-a", "user-b" };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (100m, "user-a"),
            (50m, "user-a")
        };

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        // Total: 150, fair share each: 75
        // user-a paid 150, owes 75 → balance = +75 (is owed money)
        // user-b paid 0, owes 75 → balance = -75 (owes money)
        Assert.Equal(75m, balances["user-a"]);
        Assert.Equal(-75m, balances["user-b"]);
    }

    [Fact]
    public void CalculateBalances_TwoMembers_UnequalExpenses_ReturnsCorrectBalances()
    {
        // Arrange
        var memberIds = new List<string> { "user-a", "user-b" };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (80m, "user-a"),  // user-a paid 80
            (20m, "user-b"),  // user-b paid 20
            (100m, "user-a")  // user-a paid another 100
        };

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        // Total: 200, fair share each: 100
        // user-a paid 180, owes 100 → balance = +80 (is owed 80)
        // user-b paid 20, owes 100 → balance = -80 (owes 80)
        Assert.Equal(80m, balances["user-a"]);
        Assert.Equal(-80m, balances["user-b"]);
    }

    [Fact]
    public void CalculateBalances_ThreeMembers_MixedExpenses_ReturnsCorrectBalances()
    {
        // Arrange - simulate 3-way co-parenting scenario
        var memberIds = new List<string> { "user-a", "user-b", "user-c" };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (90m, "user-a"),  // user-a paid 90
            (60m, "user-b"),  // user-b paid 60
            (30m, "user-c")   // user-c paid 30
        };

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        // Total: 180, fair share each: 60
        // user-a paid 90, owes 60 → balance = +30 (is owed 30)
        // user-b paid 60, owes 60 → balance = 0
        // user-c paid 30, owes 60 → balance = -30 (owes 30)
        Assert.Equal(30m, balances["user-a"]);
        Assert.Equal(0m, balances["user-b"]);
        Assert.Equal(-30m, balances["user-c"]);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateBalances_EmptyExpenses_ReturnsZeroBalances()
    {
        // Arrange
        var memberIds = new List<string> { "user-a", "user-b" };
        var expenses = new List<(decimal Amount, string PaidBy)>();

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        Assert.Equal(0m, balances["user-a"]);
        Assert.Equal(0m, balances["user-b"]);
    }

    [Fact]
    public void CalculateBalances_SingleMember_ReturnsEmptyDictionary()
    {
        // Arrange - can't split between 1 person
        var memberIds = new List<string> { "user-a" };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (100m, "user-a")
        };

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        Assert.Empty(balances);
    }

    [Fact]
    public void CalculateBalances_NoMembers_ReturnsEmptyDictionary()
    {
        // Arrange
        var memberIds = new List<string>();
        var expenses = new List<(decimal Amount, string PaidBy)>();

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        Assert.Empty(balances);
    }

    [Fact]
    public void CalculateBalances_DecimalAmounts_CalculatesCorrectly()
    {
        // Arrange
        var memberIds = new List<string> { "user-a", "user-b" };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (33.33m, "user-a"),
            (66.67m, "user-b")
        };

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        // Total: 100, fair share each: 50
        // user-a paid 33.33, owes 50 → balance = -16.67 (owes money)
        // user-b paid 66.67, owes 50 → balance = +16.67 (is owed money)
        Assert.Equal(-16.67m, balances["user-a"]);
        Assert.Equal(16.67m, balances["user-b"]);
    }

    #endregion

    #region Custom Split Tests

    [Fact]
    public void CalculateBalancesWithSplit_60_40_Split_ReturnsCorrectBalances()
    {
        // Arrange - one parent responsible for 60%, other for 40%
        var memberSplits = new Dictionary<string, decimal>
        {
            { "user-a", 60m },
            { "user-b", 40m }
        };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (100m, "user-a")  // user-a paid all 100
        };

        // Act
        var balances = BalanceCalculator.CalculateBalancesWithSplit(expenses, memberSplits);

        // Assert
        // Total: 100
        // user-a owes 60 (60%), paid 100 → balance = +40 (is owed 40)
        // user-b owes 40 (40%), paid 0 → balance = -40 (owes 40)
        Assert.Equal(40m, balances["user-a"]);
        Assert.Equal(-40m, balances["user-b"]);
    }

    [Fact]
    public void CalculateBalancesWithSplit_70_30_Split_ReturnsCorrectBalances()
    {
        // Arrange
        var memberSplits = new Dictionary<string, decimal>
        {
            { "user-a", 70m },
            { "user-b", 30m }
        };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (50m, "user-a"),
            (50m, "user-b")
        };

        // Act
        var balances = BalanceCalculator.CalculateBalancesWithSplit(expenses, memberSplits);

        // Assert
        // Total: 100
        // user-a owes 70, paid 50 → balance = -20 (owes 20)
        // user-b owes 30, paid 50 → balance = +20 (is owed 20)
        Assert.Equal(-20m, balances["user-a"]);
        Assert.Equal(20m, balances["user-b"]);
    }

    [Fact]
    public void CalculateBalancesWithSplit_SingleMember_ReturnsEmptyDictionary()
    {
        // Arrange
        var memberSplits = new Dictionary<string, decimal>
        {
            { "user-a", 100m }
        };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (100m, "user-a")
        };

        // Act
        var balances = BalanceCalculator.CalculateBalancesWithSplit(expenses, memberSplits);

        // Assert
        Assert.Empty(balances);
    }

    #endregion

    #region Realistic Scenarios

    [Fact]
    public void CalculateBalances_RealisticCoParentingScenario_CalculatesCorrectly()
    {
        // Arrange - typical month of co-parenting expenses
        var memberIds = new List<string> { "mom", "dad" };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (250m, "mom"),   // School supplies
            (150m, "dad"),   // Soccer registration
            (75m, "mom"),    // Doctor copay
            (45m, "dad"),    // School lunch deposit
            (200m, "mom"),   // Winter clothes
            (80m, "dad")     // Birthday party
        };

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert
        // Total: 800, fair share each: 400
        // mom paid: 250+75+200 = 525, owes 400 → balance = +125
        // dad paid: 150+45+80 = 275, owes 400 → balance = -125
        Assert.Equal(125m, balances["mom"]);
        Assert.Equal(-125m, balances["dad"]);

        // Verify balances sum to zero (conservation)
        Assert.Equal(0m, balances.Values.Sum());
    }

    [Fact]
    public void CalculateBalances_BalancesAlwaysSumToZero()
    {
        // Arrange - random-ish values
        var memberIds = new List<string> { "user-a", "user-b" };
        var expenses = new List<(decimal Amount, string PaidBy)>
        {
            (123.45m, "user-a"),
            (67.89m, "user-b"),
            (234.56m, "user-a")
        };

        // Act
        var balances = BalanceCalculator.CalculateBalances(expenses, memberIds);

        // Assert - balances should always sum to zero (law of conservation)
        Assert.Equal(0m, balances.Values.Sum());
    }

    #endregion
}
