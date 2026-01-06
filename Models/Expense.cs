namespace Denly.Models;

public enum ExpenseCategory
{
    Medical,
    School,
    Activities,
    Clothing,
    Childcare,
    Other
}

public enum Parent
{
    ParentA,
    ParentB
}

public class Expense
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public ExpenseCategory Category { get; set; } = ExpenseCategory.Other;
    public Parent PaidBy { get; set; } = Parent.ParentA;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class ExpenseCategoryExtensions
{
    public static string GetDisplayName(this ExpenseCategory category) => category switch
    {
        ExpenseCategory.Medical => "Medical",
        ExpenseCategory.School => "School",
        ExpenseCategory.Activities => "Activities",
        ExpenseCategory.Clothing => "Clothing",
        ExpenseCategory.Childcare => "Childcare",
        ExpenseCategory.Other => "Other",
        _ => "Other"
    };

    public static string GetColor(this ExpenseCategory category) => category switch
    {
        ExpenseCategory.Medical => "#81b29a",    // Sage green
        ExpenseCategory.School => "#f2cc8f",     // Soft gold
        ExpenseCategory.Activities => "#3d85c6", // Calm blue
        ExpenseCategory.Clothing => "#a78bba",   // Soft purple
        ExpenseCategory.Childcare => "#e07a5f",  // Warm terracotta
        ExpenseCategory.Other => "#9ca3af",      // Neutral gray
        _ => "#9ca3af"
    };
}

public static class ParentExtensions
{
    public static string GetDisplayName(this Parent parent) => parent switch
    {
        Parent.ParentA => "Parent A",
        Parent.ParentB => "Parent B",
        _ => "Unknown"
    };
}
