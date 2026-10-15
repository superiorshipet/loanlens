using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LoanLens.Infrastructure.Services;

public class LoanPdfReportService : IPdfReportService
{
    static LoanPdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;

        var fontPaths = new[]
        {
            "/usr/share/fonts/truetype/noto/NotoSansArabic-Regular.ttf",
            "/usr/share/fonts/truetype/noto/NotoSansArabic-Bold.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
        };

        foreach (var path in fontPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    using var stream = File.OpenRead(path);
                    QuestPDF.Drawing.FontManager.RegisterFontFromStream(stream);
                }
                catch
                {
                    // Fallback to system fonts
                }
            }
        }
    }

    public byte[] GenerateReportPdf(ComprehensiveLoanReportDto report, string? customerName = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Noto Sans Arabic", "DejaVu Sans", "Arial").FontSize(10).FontColor(Colors.Grey.Darken3));

                // Header
                page.Header().Element(c => ComposeHeader(c, report, customerName));

                // Content
                page.Content().Element(c => ComposeContent(c, report));

                // Footer
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ComprehensiveLoanReportDto report, string? customerName)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("LoanLens | Financial Advisor")
                   .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                col.Item().Text("تقرير التحليل المالي ومخاطر الائتمان الاستهلاكي")
                   .FontSize(11).FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"التاريخ: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                   .FontSize(9).FontColor(Colors.Grey.Medium);
                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    col.Item().Text($"العميل: {customerName}")
                       .FontSize(10).Bold().FontColor(Colors.Grey.Darken3);
                }
            });
        });
    }

    private static void ComposeContent(IContainer container, ComprehensiveLoanReportDto report)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(12);

            // 1. Verdict & Risk Score Card
            column.Item().Element(c => ComposeVerdictBanner(c, report));

            // 2. Financial Metrics Matrix (4 Key Cards)
            column.Item().Element(c => ComposeMetricsGrid(c, report));

            // 3. Stress Testing Summary Table
            column.Item().Element(c => ComposeStressTestTable(c, report));

            // 4. Monte Carlo Simulation Highlights
            column.Item().Element(c => ComposeMonteCarloSection(c, report));

            // 5. Alternatives Comparison Table
            if (report.Recommendation.TopAlternatives.Count > 0)
            {
                column.Item().Element(c => ComposeAlternativesTable(c, report));
            }

            // 6. Actionable Advice
            column.Item().Element(c => ComposeAdviceSection(c, report));
        });
    }

    private static void ComposeVerdictBanner(IContainer container, ComprehensiveLoanReportDto report)
    {
        var risk = report.RiskEvaluation;
        var verdict = report.Recommendation;

        var (bgColor, textColor) = risk.Category switch
        {
            RiskLevel.Safe => (Colors.Green.Lighten5, Colors.Green.Darken3),
            RiskLevel.Moderate => (Colors.Orange.Lighten5, Colors.Orange.Darken3),
            RiskLevel.High => (Colors.Amber.Lighten5, Colors.Amber.Darken4),
            _ => (Colors.Red.Lighten5, Colors.Red.Darken3)
        };

        container.Border(1).BorderColor(textColor).Background(bgColor).Padding(12).Row(row =>
        {
            row.RelativeItem(3).Column(col =>
            {
                col.Item().Text(verdict.VerdictHeadline).FontSize(13).Bold().FontColor(textColor);
                col.Item().PaddingTop(3).Text(verdict.DetailedReasoning).FontSize(9.5f).FontColor(Colors.Grey.Darken3);
            });

            row.RelativeItem(1).AlignRight().Column(col =>
            {
                col.Item().Text("مؤشر المخاطر").FontSize(9).FontColor(textColor);
                col.Item().Text($"{risk.TotalRiskScore:0}/100").FontSize(22).Bold().FontColor(textColor);
                col.Item().Text(risk.RiskLabel).FontSize(9).Bold().FontColor(textColor);
            });
        });
    }

    private static void ComposeMetricsGrid(IContainer container, ComprehensiveLoanReportDto report)
    {
        container.Row(row =>
        {
            row.Spacing(8);

            // Card 1: Principal & Installment
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
            {
                col.Item().Text("مبلغ القرض المطلوب").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.LoanDetails.Principal:N0} ج.م").FontSize(12).Bold();
                col.Item().PaddingTop(4).Text("القسط الشهري").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.AmortizationSchedule.MonthlyInstallment:N0} ج.م").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            });

            // Card 2: DTI & Cash Flow
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
            {
                col.Item().Text("نسبة عبء الدين (DTI)").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.Affordability.DtiPercentage:0.0}%").FontSize(12).Bold()
                   .FontColor(report.Affordability.DtiPercentage > 40 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                col.Item().PaddingTop(4).Text("الدخل المتبقي بعد القسط").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.Affordability.DisposableIncomeAfterInstallment:N0} ج.م").FontSize(12).Bold();
            });

            // Card 3: Rates & Term
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
            {
                col.Item().Text("المدة وسعر الفائدة").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.LoanDetails.TenorMonths} شهر @ {report.LoanDetails.AnnualInterestRatePercentage}%").FontSize(10.5f).Bold();
                col.Item().PaddingTop(4).Text("الفائدة الفعلية (APR)").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.AmortizationSchedule.EffectiveAnnualRatePercentage:0.0}%").FontSize(11).Bold().FontColor(Colors.Purple.Darken2);
            });

            // Card 4: Runway & Total Interest
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
            {
                col.Item().Text("شهور الأمان (Runway)").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.Affordability.EmergencyRunwayMonths:0.0} شهور").FontSize(12).Bold();
                col.Item().PaddingTop(4).Text("إجمالي الفائدة المدفوعة").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.AmortizationSchedule.TotalInterest:N0} ج.م").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
            });
        });
    }

    private static void ComposeStressTestTable(IContainer container, ComprehensiveLoanReportDto report)
    {
        container.Column(col =>
        {
            col.Item().Text("نتائج محاكاة الصدمات المالية (Stress Testing)").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Scenario
                    columns.RelativeColumn(1); // Status
                    columns.RelativeColumn(4); // Impact
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("السيناريو").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("الحالة").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("التأثير والأثر على المدخرات").Bold().FontSize(8.5f);
                });

                foreach (var s in report.StressTest.Scenarios)
                {
                    var statusColor = s.IsSurviving ? Colors.Green.Darken2 : Colors.Red.Darken2;
                    var statusText = s.IsSurviving ? "نجاح" : "تعثر";

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(s.ScenarioName).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(statusText).Bold().FontColor(statusColor).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(s.ImpactDescription).FontSize(8.5f);
                }
            });
        });
    }

    private static void ComposeMonteCarloSection(IContainer container, ComprehensiveLoanReportDto report)
    {
        var mc = report.MonteCarlo;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text($"محاكاة مونت كارلو العشوائية ({mc.TotalIterations:N0} مسار محاكى)").FontSize(10.5f).Bold().FontColor(Colors.Blue.Darken3);

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"احتمالية التعثر: {mc.DefaultProbabilityPercentage:0.0}%")
                   .Bold().FontColor(mc.DefaultProbabilityPercentage > 10 ? Colors.Red.Darken2 : Colors.Green.Darken2);

                row.RelativeItem().Text($"أخطر شهر متوقع: {(mc.PeakRiskMonth.HasValue ? $"الشهر {mc.PeakRiskMonth}" : "لا يوجد")}")
                   .FontSize(9).FontColor(Colors.Grey.Darken3);

                row.RelativeItem().Text($"متوسط الرصيد النهائي: {mc.MedianEndingSavings:N0} ج.م")
                   .FontSize(9).FontColor(Colors.Grey.Darken3);
            });
        });
    }

    private static void ComposeAlternativesTable(IContainer container, ComprehensiveLoanReportDto report)
    {
        container.Column(col =>
        {
            col.Item().Text("مقارنة البدائل المتاحة في السوق المصري").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Provider
                    columns.RelativeColumn(1.5f); // Installment
                    columns.RelativeColumn(1.5f); // APR
                    columns.RelativeColumn(2); // Diff
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("الجهة والمنتج").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("القسط الشهري").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("الفائدة الفعلية").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("مقارنة بالقرض الحالي").Bold().FontSize(8.5f);
                });

                foreach (var alt in report.Recommendation.TopAlternatives.Take(4))
                {
                    var diffText = alt.IsCheaperThanCurrent
                        ? $"أرخص بـ {alt.CostDifferenceComparedToCurrent:N0} ج.م"
                        : $"أغلى بـ {alt.CostDifferenceComparedToCurrent:N0} ج.م";
                    var diffColor = alt.IsCheaperThanCurrent ? Colors.Green.Darken2 : Colors.Grey.Darken2;

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{alt.Product.ProviderName}").FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{alt.MonthlyInstallment:N0} ج.م").FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{alt.EffectiveAprPercentage:0.0}%").FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(diffText).Bold().FontColor(diffColor).FontSize(8.5f);
                }
            });
        });
    }

    private static void ComposeAdviceSection(IContainer container, ComprehensiveLoanReportDto report)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
        {
            col.Item().Text("نصائح عملية هامة").FontSize(10).Bold().FontColor(Colors.Blue.Darken3);
            foreach (var advice in report.Recommendation.KeyActionableAdvice)
            {
                col.Item().PaddingTop(2).Text($"• {advice}").FontSize(8.5f);
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem(3).Text("إخلاء مسؤولية: هذا التقرير أداة استرشادية مساعدة للقرار المالي ولا يعد استشارة رسمية ملزمة. البيانات المالية سرية ومحمية.")
                   .FontSize(7.5f).FontColor(Colors.Grey.Medium);
                row.RelativeItem(1).AlignRight().Text(x =>
                {
                    x.Span("صفحة ");
                    x.CurrentPageNumber();
                    x.Span(" من ");
                    x.TotalPages();
                });
            });
        });
    }
}
