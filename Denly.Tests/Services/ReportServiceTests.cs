using Denly.Models;
using Denly.Services;
using NSubstitute;
using System.Text;

namespace Denly.Tests.Services;

/// <summary>
/// Tests for ReportService expense report generation.
/// </summary>
public class ReportServiceTests
{
    private readonly IExpenseService _mockExpenseService;
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        _mockExpenseService = Substitute.For<IExpenseService>();
        _reportService = new ReportService(_mockExpenseService);
    }

    #region CSV Generation Tests

    [Fact]
    public async Task GenerateExpenseReportCsvAsync_ReturnsValidCsvHeaders()
    {
        // Arrange
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Expense>());

        // Act
        var result = await _reportService.GenerateExpenseReportCsvAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);
        var csv = Encoding.UTF8.GetString(result);

        // Assert
        Assert.StartsWith("Date,Description,Amount,Paid By,Split %", csv);
    }

    [Fact]
    public async Task GenerateExpenseReportCsvAsync_IncludesExpenseData()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new Expense
            {
                Id = "1",
                Description = "School supplies",
                Amount = 50.00m,
                PaidBy = "user-a",
                PaidByName = "Mom",
                SplitPercent = 50m,
                CreatedAt = DateTime.Today.AddDays(-5)
            }
        };
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(expenses);

        // Act
        var result = await _reportService.GenerateExpenseReportCsvAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);
        var csv = Encoding.UTF8.GetString(result);

        // Assert
        Assert.Contains("School supplies", csv);
        Assert.Contains("50", csv);
        Assert.Contains("Mom", csv);
    }

    [Fact]
    public async Task GenerateExpenseReportCsvAsync_EscapesQuotesInDescription()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new Expense
            {
                Id = "1",
                Description = "Book \"The Cat in the Hat\"",
                Amount = 15.00m,
                PaidBy = "user-a",
                PaidByName = "Dad",
                SplitPercent = 50m,
                CreatedAt = DateTime.Today.AddDays(-1)
            }
        };
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(expenses);

        // Act
        var result = await _reportService.GenerateExpenseReportCsvAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);
        var csv = Encoding.UTF8.GetString(result);

        // Assert - quotes should be escaped by doubling
        Assert.Contains("\"\"The Cat in the Hat\"\"", csv);
    }

    [Fact]
    public async Task GenerateExpenseReportCsvAsync_FiltersExpensesByDateRange()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new Expense
            {
                Id = "1",
                Description = "In range",
                Amount = 10m,
                PaidByName = "Mom",
                CreatedAt = DateTime.Today.AddDays(-5)
            },
            new Expense
            {
                Id = "2",
                Description = "Out of range",
                Amount = 20m,
                PaidByName = "Dad",
                CreatedAt = DateTime.Today.AddDays(-60)
            }
        };
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(expenses);

        // Act
        var result = await _reportService.GenerateExpenseReportCsvAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);
        var csv = Encoding.UTF8.GetString(result);

        // Assert
        Assert.Contains("In range", csv);
        Assert.DoesNotContain("Out of range", csv);
    }

    [Fact]
    public async Task GenerateExpenseReportCsvAsync_HandlesNullDescription()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new Expense
            {
                Id = "1",
                Description = null!,
                Amount = 25m,
                PaidByName = "Mom",
                CreatedAt = DateTime.Today.AddDays(-1)
            }
        };
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(expenses);

        // Act
        var result = await _reportService.GenerateExpenseReportCsvAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);
        var csv = Encoding.UTF8.GetString(result);

        // Assert - should not throw and should have empty description
        Assert.Contains(",\"\",25", csv);
    }

    [Fact]
    public async Task GenerateExpenseReportCsvAsync_HandlesNullPaidByName()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new Expense
            {
                Id = "1",
                Description = "Test",
                Amount = 25m,
                PaidByName = null,
                CreatedAt = DateTime.Today.AddDays(-1)
            }
        };
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(expenses);

        // Act
        var result = await _reportService.GenerateExpenseReportCsvAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);
        var csv = Encoding.UTF8.GetString(result);

        // Assert - should show "Unknown" for null payer
        Assert.Contains("Unknown", csv);
    }

    #endregion

    #region PDF Generation Tests

    [Fact]
    public async Task GenerateExpenseReportPdfAsync_ReturnsNonEmptyByteArray()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new Expense
            {
                Id = "1",
                Description = "Test expense",
                Amount = 100m,
                CreatedAt = DateTime.Today.AddDays(-1)
            }
        };
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(expenses);

        // Act
        var result = await _reportService.GenerateExpenseReportPdfAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GenerateExpenseReportPdfAsync_StartsWithPdfMagicBytes()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new Expense
            {
                Id = "1",
                Description = "Test",
                Amount = 50m,
                CreatedAt = DateTime.Today.AddDays(-1)
            }
        };
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(expenses);

        // Act
        var result = await _reportService.GenerateExpenseReportPdfAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);

        // Assert - PDF files start with %PDF
        var header = Encoding.ASCII.GetString(result, 0, 4);
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task GenerateExpenseReportPdfAsync_HandlesEmptyExpenseList()
    {
        // Arrange
        _mockExpenseService.GetExpensesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Expense>());

        // Act
        var result = await _reportService.GenerateExpenseReportPdfAsync(
            DateTime.Today.AddDays(-30),
            DateTime.Today);

        // Assert - should still produce valid PDF
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var header = Encoding.ASCII.GetString(result, 0, 4);
        Assert.Equal("%PDF", header);
    }

    #endregion
}
