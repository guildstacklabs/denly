namespace Denly.Components.Shared;

public class SharedCostsExpenseItem
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string SplitText { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string? PayerInitials { get; set; }
    public string? ChildTag { get; set; }
}
