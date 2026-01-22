using System;
using System.Threading.Tasks;

namespace Denly.Services;

public interface IReportService
{
    Task<byte[]> GenerateExpenseReportCsvAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateExpenseReportPdfAsync(DateTime startDate, DateTime endDate);
}