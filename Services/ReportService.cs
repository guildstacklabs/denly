using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Denly.Models;

namespace Denly.Services;

public class ReportService : IReportService
{
    private readonly IExpenseService _expenseService;

    public ReportService(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task<byte[]> GenerateExpenseReportCsvAsync(DateTime startDate, DateTime endDate)
    {
        // Assuming GetExpensesAsync accepts date range or we filter client-side
        // For this implementation, we assume a method signature or filter locally
        var allExpenses = await _expenseService.GetExpensesAsync(); 
        var expenses = allExpenses.Where(e => e.Date >= startDate && e.Date <= endDate).OrderBy(e => e.Date).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Date,Description,Amount,Paid By,Split %");

        foreach (var expense in expenses)
        {
            // Escape quotes in description
            var desc = expense.Description?.Replace("\"", "\"\"") ?? "";
            var paidBy = expense.PaidByName ?? "Unknown";
            sb.AppendLine($"{expense.Date:yyyy-MM-dd},\"{desc}\",{expense.Amount},{paidBy},{expense.SplitPercent}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GenerateExpenseReportPdfAsync(DateTime startDate, DateTime endDate)
    {
        var allExpenses = await _expenseService.GetExpensesAsync();
        var expenses = allExpenses.Where(e => e.Date >= startDate && e.Date <= endDate).OrderBy(e => e.Date).ToList();

        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        
        using var paint = new SKPaint
        {
            TextSize = 12,
            IsAntialias = true,
            Color = SKColors.Black,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        var canvas = document.BeginPage(595, 842); // A4 size
        float y = 50;
        float x = 50;
        
        canvas.DrawText($"Expense Report: {startDate:d} - {endDate:d}", x, y, paint);
        y += 30;
        
        foreach (var expense in expenses)
        {
            if (y > 800)
            {
                document.EndPage();
                canvas = document.BeginPage(595, 842);
                y = 50;
            }

            var line = $"{expense.Date:MM/dd} - {expense.Description} - ${expense.Amount}";
            canvas.DrawText(line, x, y, paint);
            y += 20;
        }
        
        document.EndPage();
        document.Close();
        
        return stream.ToArray();
    }
}