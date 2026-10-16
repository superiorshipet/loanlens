using ClosedXML.Excel;
using LoanLens.Application.Interfaces;
using LoanLens.Domain.Localization;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Infrastructure.Services;

public class LoanExcelExportService : IExcelExportService
{
    public byte[] ExportAmortizationScheduleToExcel(
        LoanParameters loan, 
        AmortizationSchedule schedule, 
        string? customerName = null, 
        string language = "en")
    {
        ArgumentNullException.ThrowIfNull(loan);
        ArgumentNullException.ThrowIfNull(schedule);

        var isAr = LoanLensLanguage.IsArabic(language);
        using var workbook = new XLWorkbook();
        var sheetName = isAr ? "جدول سداد القرض" : "Amortization Schedule";
        var ws = workbook.Worksheets.Add(sheetName);

        if (isAr)
        {
            ws.RightToLeft = true;
        }

        // Title Row
        ws.Cell(1, 1).Value = "LoanLens | Financial Loan Advisory";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(26, 86, 219);

        ws.Cell(2, 1).Value = isAr ? "جدول استهلاك وسداد القرض المفصل" : "Detailed Loan Amortization Schedule";
        ws.Cell(2, 1).Style.Font.FontSize = 11;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.DarkGray;

        // Metadata Block
        int r = 4;
        void AddMeta(string label, object val)
        {
            ws.Cell(r, 1).Value = label;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.DimGray;
            ws.Cell(r, 2).Value = val.ToString();
            ws.Cell(r, 2).Style.Font.Bold = true;
            r++;
        }

        if (!string.IsNullOrWhiteSpace(customerName))
            AddMeta(isAr ? "اسم العميل:" : "Client Name:", customerName);

        AddMeta(isAr ? "مبلغ القرض المطلوب:" : "Loan Principal:", $"{loan.Principal:N2} EGP");
        AddMeta(isAr ? "مدة القرض:" : "Tenor:", isAr ? $"{loan.TenorMonths} شهر" : $"{loan.TenorMonths} Months");
        AddMeta(isAr ? "نسبة الفائدة السنوية:" : "Annual Interest Rate:", $"{loan.AnnualInterestRatePercentage}% ({loan.CalculationMethod})");
        AddMeta(isAr ? "الفائدة الفعلية السنوية (APR):" : "Effective APR:", $"{schedule.EffectiveAnnualRatePercentage:0.0}%");
        AddMeta(isAr ? "القسط الشهري الثابت:" : "Monthly Installment:", $"{schedule.MonthlyInstallment:N2} EGP");
        AddMeta(isAr ? "إجمالي الفوائد المستحقة:" : "Total Interest Paid:", $"{schedule.TotalInterest:N2} EGP");
        AddMeta(isAr ? "إجمالي المبلغ المسدد:" : "Total Repayment:", $"{schedule.TotalPayment:N2} EGP");

        r += 1;

        // Table Header
        int tableHeaderRow = r;
        var headers = isAr
            ? new[] { "رقم القسط", "رصيد أول المدة", "قيمة القسط", "أصل القرض", "الفائدة المستحقة", "رصيد آخر المدة" }
            : new[] { "Month #", "Beginning Balance", "Installment", "Principal Portion", "Interest Portion", "Ending Balance" };

        for (int c = 1; c <= headers.Length; c++)
        {
            var cell = ws.Cell(tableHeaderRow, c);
            cell.Value = headers[c - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(30, 64, 175);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Table Data Rows
        int dataStartRow = tableHeaderRow + 1;
        int rowIdx = dataStartRow;

        foreach (var item in schedule.Items)
        {
            ws.Cell(rowIdx, 1).Value = item.Month;
            ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(rowIdx, 2).Value = (double)item.BeginningBalance;
            ws.Cell(rowIdx, 2).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(rowIdx, 3).Value = (double)item.InstallmentAmount;
            ws.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(rowIdx, 4).Value = (double)item.PrincipalPortion;
            ws.Cell(rowIdx, 4).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(rowIdx, 5).Value = (double)item.InterestPortion;
            ws.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(rowIdx, 6).Value = (double)item.EndingBalance;
            ws.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0.00";

            if (rowIdx % 2 == 0)
            {
                ws.Range(rowIdx, 1, rowIdx, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
            }

            rowIdx++;
        }

        // Summary Total Row
        int totalRow = rowIdx;
        ws.Cell(totalRow, 1).Value = isAr ? "الإجمالي" : "Total";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(totalRow, 3).FormulaA1 = $"SUM(C{dataStartRow}:C{totalRow - 1})";
        ws.Cell(totalRow, 3).Style.Font.Bold = true;
        ws.Cell(totalRow, 3).Style.NumberFormat.Format = "#,##0.00";

        ws.Cell(totalRow, 4).FormulaA1 = $"SUM(D{dataStartRow}:D{totalRow - 1})";
        ws.Cell(totalRow, 4).Style.Font.Bold = true;
        ws.Cell(totalRow, 4).Style.NumberFormat.Format = "#,##0.00";

        ws.Cell(totalRow, 5).FormulaA1 = $"SUM(E{dataStartRow}:E{totalRow - 1})";
        ws.Cell(totalRow, 5).Style.Font.Bold = true;
        ws.Cell(totalRow, 5).Style.NumberFormat.Format = "#,##0.00";

        ws.Range(totalRow, 1, totalRow, 6).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(totalRow, 1, totalRow, 6).Style.Border.BottomBorder = XLBorderStyleValues.Double;
        ws.Range(totalRow, 1, totalRow, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(241, 245, 249);

        // Auto-fit columns
        ws.Columns(1, 6).AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
