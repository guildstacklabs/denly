using Denly.Models;

namespace Denly.Tests.Services;

/// <summary>
/// Tests for multi-child association logic.
/// Verifies that Event and Expense ChildIds behave correctly,
/// and that child-based filtering works as expected.
/// </summary>
public class ChildAssociationTests
{
    #region Event ChildIds Tests

    [Fact]
    public void Event_ChildIds_DefaultsToEmptyList()
    {
        var evt = new Event();
        Assert.NotNull(evt.ChildIds);
        Assert.Empty(evt.ChildIds);
    }

    [Fact]
    public void Event_ChildIds_CanBePopulated()
    {
        var evt = new Event();
        evt.ChildIds = new List<string> { "child-1", "child-2" };

        Assert.Equal(2, evt.ChildIds.Count);
        Assert.Contains("child-1", evt.ChildIds);
        Assert.Contains("child-2", evt.ChildIds);
    }

    [Fact]
    public void Event_ChildIds_IndependentOfLegacyChildId()
    {
        var evt = new Event
        {
            ChildId = "legacy-child",
            ChildIds = new List<string> { "child-1", "child-2" }
        };

        Assert.Equal("legacy-child", evt.ChildId);
        Assert.Equal(2, evt.ChildIds.Count);
        Assert.DoesNotContain("legacy-child", evt.ChildIds);
    }

    #endregion

    #region Expense ChildIds Tests

    [Fact]
    public void Expense_ChildIds_DefaultsToEmptyList()
    {
        var expense = new Expense();
        Assert.NotNull(expense.ChildIds);
        Assert.Empty(expense.ChildIds);
    }

    [Fact]
    public void Expense_ChildIds_CanBePopulated()
    {
        var expense = new Expense();
        expense.ChildIds = new List<string> { "child-a", "child-b", "child-c" };

        Assert.Equal(3, expense.ChildIds.Count);
    }

    #endregion

    #region EventChild Model Tests

    [Fact]
    public void EventChild_DefaultValues_AreCorrect()
    {
        var ec = new EventChild();

        Assert.NotNull(ec.Id);
        Assert.NotEmpty(ec.Id);
        Assert.Equal(string.Empty, ec.EventId);
        Assert.Equal(string.Empty, ec.ChildId);
        Assert.Equal(string.Empty, ec.DenId);
    }

    [Fact]
    public void EventChild_CanSetProperties()
    {
        var ec = new EventChild
        {
            EventId = "event-1",
            ChildId = "child-1",
            DenId = "den-1"
        };

        Assert.Equal("event-1", ec.EventId);
        Assert.Equal("child-1", ec.ChildId);
        Assert.Equal("den-1", ec.DenId);
    }

    #endregion

    #region ExpenseChild Model Tests

    [Fact]
    public void ExpenseChild_DefaultValues_AreCorrect()
    {
        var ec = new ExpenseChild();

        Assert.NotNull(ec.Id);
        Assert.NotEmpty(ec.Id);
        Assert.Equal(string.Empty, ec.ExpenseId);
        Assert.Equal(string.Empty, ec.ChildId);
        Assert.Equal(string.Empty, ec.DenId);
    }

    [Fact]
    public void ExpenseChild_CanSetProperties()
    {
        var ec = new ExpenseChild
        {
            ExpenseId = "expense-1",
            ChildId = "child-1",
            DenId = "den-1"
        };

        Assert.Equal("expense-1", ec.ExpenseId);
        Assert.Equal("child-1", ec.ChildId);
        Assert.Equal("den-1", ec.DenId);
    }

    #endregion

    #region Child Filtering Logic Tests

    [Fact]
    public void FilterEventsByChild_AllFilter_ReturnsAllEvents()
    {
        var events = CreateTestEvents();

        // "all" filter — no filtering
        var filtered = FilterEventsByChild(events, null);

        Assert.Equal(3, filtered.Count);
    }

    [Fact]
    public void FilterEventsByChild_SpecificChild_ReturnsMatchingEvents()
    {
        var events = CreateTestEvents();

        var filtered = FilterEventsByChild(events, "child-1");

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, e => Assert.Contains("child-1", e.ChildIds));
    }

    [Fact]
    public void FilterEventsByChild_NoMatches_ReturnsEmpty()
    {
        var events = CreateTestEvents();

        var filtered = FilterEventsByChild(events, "child-nonexistent");

        Assert.Empty(filtered);
    }

    [Fact]
    public void FilterEventsByChild_EventWithNoChildren_ExcludedByChildFilter()
    {
        var events = new List<Event>
        {
            new() { Id = "e1", ChildIds = new List<string>() },
            new() { Id = "e2", ChildIds = new List<string> { "child-1" } }
        };

        var filtered = FilterEventsByChild(events, "child-1");

        Assert.Single(filtered);
        Assert.Equal("e2", filtered[0].Id);
    }

    [Fact]
    public void FilterExpensesByChild_SpecificChild_ReturnsMatchingExpenses()
    {
        var expenses = new List<Expense>
        {
            new() { Id = "exp-1", ChildIds = new List<string> { "child-1" } },
            new() { Id = "exp-2", ChildIds = new List<string> { "child-2" } },
            new() { Id = "exp-3", ChildIds = new List<string> { "child-1", "child-2" } }
        };

        var filtered = FilterExpensesByChild(expenses, "child-1");

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, e => e.Id == "exp-1");
        Assert.Contains(filtered, e => e.Id == "exp-3");
    }

    [Fact]
    public void FilterExpensesByChild_AllFilter_ReturnsAllExpenses()
    {
        var expenses = new List<Expense>
        {
            new() { Id = "exp-1", ChildIds = new List<string> { "child-1" } },
            new() { Id = "exp-2", ChildIds = new List<string> { "child-2" } }
        };

        var filtered = FilterExpensesByChild(expenses, null);

        Assert.Equal(2, filtered.Count);
    }

    #endregion

    #region Mapping EventChild Records to Event.ChildIds

    [Fact]
    public void MapEventChildren_PopulatesChildIdsOnEvents()
    {
        var events = new List<Event>
        {
            new() { Id = "e1" },
            new() { Id = "e2" },
            new() { Id = "e3" }
        };

        var eventChildren = new List<EventChild>
        {
            new() { EventId = "e1", ChildId = "child-a" },
            new() { EventId = "e1", ChildId = "child-b" },
            new() { EventId = "e2", ChildId = "child-a" },
        };

        MapEventChildren(events, eventChildren);

        Assert.Equal(2, events[0].ChildIds.Count);
        Assert.Contains("child-a", events[0].ChildIds);
        Assert.Contains("child-b", events[0].ChildIds);
        Assert.Single(events[1].ChildIds);
        Assert.Empty(events[2].ChildIds);
    }

    [Fact]
    public void MapExpenseChildren_PopulatesChildIdsOnExpenses()
    {
        var expenses = new List<Expense>
        {
            new() { Id = "exp-1" },
            new() { Id = "exp-2" }
        };

        var expenseChildren = new List<ExpenseChild>
        {
            new() { ExpenseId = "exp-1", ChildId = "child-x" },
            new() { ExpenseId = "exp-1", ChildId = "child-y" },
            new() { ExpenseId = "exp-2", ChildId = "child-x" },
        };

        MapExpenseChildren(expenses, expenseChildren);

        Assert.Equal(2, expenses[0].ChildIds.Count);
        Assert.Single(expenses[1].ChildIds);
    }

    [Fact]
    public void MapEventChildren_EmptyList_LeavesChildIdsEmpty()
    {
        var events = new List<Event> { new() { Id = "e1" } };
        MapEventChildren(events, new List<EventChild>());

        Assert.Empty(events[0].ChildIds);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Mirrors the filtering logic used in Calendar.razor and Expenses.razor.
    /// </summary>
    private static List<Event> FilterEventsByChild(List<Event> events, string? childId)
    {
        if (string.IsNullOrEmpty(childId))
            return events;

        return events.Where(e => e.ChildIds.Contains(childId)).ToList();
    }

    private static List<Expense> FilterExpensesByChild(List<Expense> expenses, string? childId)
    {
        if (string.IsNullOrEmpty(childId))
            return expenses;

        return expenses.Where(e => e.ChildIds.Contains(childId)).ToList();
    }

    /// <summary>
    /// Mirrors the mapping logic used after loading events from the service.
    /// </summary>
    private static void MapEventChildren(List<Event> events, List<EventChild> eventChildren)
    {
        var lookup = eventChildren.GroupBy(ec => ec.EventId)
            .ToDictionary(g => g.Key, g => g.Select(ec => ec.ChildId).ToList());

        foreach (var evt in events)
        {
            evt.ChildIds = lookup.TryGetValue(evt.Id, out var ids) ? ids : new List<string>();
        }
    }

    private static void MapExpenseChildren(List<Expense> expenses, List<ExpenseChild> expenseChildren)
    {
        var lookup = expenseChildren.GroupBy(ec => ec.ExpenseId)
            .ToDictionary(g => g.Key, g => g.Select(ec => ec.ChildId).ToList());

        foreach (var expense in expenses)
        {
            expense.ChildIds = lookup.TryGetValue(expense.Id, out var ids) ? ids : new List<string>();
        }
    }

    private static List<Event> CreateTestEvents()
    {
        return new List<Event>
        {
            new() { Id = "e1", ChildIds = new List<string> { "child-1", "child-2" } },
            new() { Id = "e2", ChildIds = new List<string> { "child-2" } },
            new() { Id = "e3", ChildIds = new List<string> { "child-1" } }
        };
    }

    #endregion
}
