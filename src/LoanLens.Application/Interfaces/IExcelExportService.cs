using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.Interfaces;

public interface IExcelExportService
{
    byte[] ExportAmortizationScheduleToExcel(
        LoanParameters loan, 
        AmortizationSchedule schedule, 
        string? customerName = null, 
        string language = "en");
}
