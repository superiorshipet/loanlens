using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;
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

    public byte[] GenerateReportPdf(ComprehensiveLoanReportDto report, string? customerName = null, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(report);

        var isAr = LoanLensLanguage.IsArabic(language);
        var currency = isAr ? "ج.م" : "EGP";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Noto Sans Arabic", "DejaVu Sans", "Arial").FontSize(10).FontColor(Colors.Grey.Darken3));

                // Header
                page.Header().Element(c => ComposeHeader(c, report, customerName, isAr));

                // Content
                page.Content().Element(c => ComposeContent(c, report, isAr, currency));

                // Footer
                page.Footer().Element(c => ComposeFooter(c, isAr));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ComprehensiveLoanReportDto report, string? customerName, bool isAr)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("LoanLens | Financial Advisor")
                   .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                col.Item().Text(isAr ? "تقرير التحليل المالي ومخاطر الائتمان الاستهلاكي" : "Consumer Credit Risk & Financial Feasibility Report")
                   .FontSize(11).FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text(isAr ? $"التاريخ: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC" : $"Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                   .FontSize(9).FontColor(Colors.Grey.Medium);
                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    col.Item().Text(isAr ? $"العميل: {customerName}" : $"Client: {customerName}")
                       .FontSize(10).Bold().FontColor(Colors.Grey.Darken3);
                }
            });
        });
    }

    private static void ComposeContent(IContainer container, ComprehensiveLoanReportDto report, bool isAr, string currency)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(12);

            // 1. Verdict & Risk Score Card
            column.Item().Element(c => ComposeVerdictBanner(c, report, isAr));

            // 2. Financial Metrics Matrix (4 Key Cards)
            column.Item().Element(c => ComposeMetricsGrid(c, report, isAr, currency));

            // 3. Stress Testing Summary Table
            column.Item().Element(c => ComposeStressTestTable(c, report, isAr));

            // 4. Monte Carlo Simulation Highlights
            column.Item().Element(c => ComposeMonteCarloSection(c, report, isAr, currency));

            // 5. Alternatives Comparison Table
            if (report.Recommendation.TopAlternatives.Count > 0)
            {
                column.Item().Element(c => ComposeAlternativesTable(c, report, isAr, currency));
            }

            // 6. Repayment Strategies Comparison Table
            if (report.Recommendation.Strategies.Count > 0)
            {
                column.Item().Element(c => ComposeRepaymentStrategiesTable(c, report, isAr, currency));
            }

            // 7. Actionable Advice
            column.Item().Element(c => ComposeAdviceSection(c, report, isAr));

            // 8. AI Narrative Advisor Briefing (if available)
            if (report.AiNarrative != null)
            {
                column.Item().Element(c => ComposeAiNarrativeSection(c, report.AiNarrative, isAr));
            }
        });
    }

    private static void ComposeVerdictBanner(IContainer container, ComprehensiveLoanReportDto report, bool isAr)
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
                col.Item().Text(isAr ? "مؤشر المخاطر" : "Risk Index").FontSize(9).FontColor(textColor);
                col.Item().Text($"{risk.TotalRiskScore:0}/100").FontSize(22).Bold().FontColor(textColor);
                col.Item().Text(risk.RiskLabel).FontSize(9).Bold().FontColor(textColor);
            });
        });
    }

    private static void ComposeMetricsGrid(IContainer container, ComprehensiveLoanReportDto report, bool isAr, string currency)
    {
        container.Row(row =>
        {
            row.Spacing(8);

            // Card 1: Principal & Installment
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
            {
                col.Item().Text(isAr ? "مبلغ القرض المطلوب" : "Requested Principal").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.LoanDetails.Principal:N0} {currency}").FontSize(12).Bold();
                col.Item().PaddingTop(4).Text(isAr ? "القسط الشهري" : "Monthly Installment").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.AmortizationSchedule.MonthlyInstallment:N0} {currency}").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            });

            // Card 2: DTI & Cash Flow
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
            {
                col.Item().Text(isAr ? "نسبة عبء الدين (DTI)" : "Debt-to-Income (DTI)").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.Affordability.DtiPercentage:0.0}%").FontSize(12).Bold()
                   .FontColor(report.Affordability.DtiPercentage > 40 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                col.Item().PaddingTop(4).Text(isAr ? "الدخل المتبقي بعد القسط" : "Net Disposable Income").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.Affordability.DisposableIncomeAfterInstallment:N0} {currency}").FontSize(12).Bold();
            });

            // Card 3: Rates & Term
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
            {
                col.Item().Text(isAr ? "المدة وسعر الفائدة" : "Tenor & Annual Rate").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text(isAr ? $"{report.LoanDetails.TenorMonths} شهر @ {report.LoanDetails.AnnualInterestRatePercentage}%" : $"{report.LoanDetails.TenorMonths} Mos @ {report.LoanDetails.AnnualInterestRatePercentage}%").FontSize(10.5f).Bold();
                col.Item().PaddingTop(4).Text(isAr ? "الفائدة الفعلية (APR)" : "Effective Rate (APR)").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.AmortizationSchedule.EffectiveAnnualRatePercentage:0.0}%").FontSize(11).Bold().FontColor(Colors.Purple.Darken2);
            });

            // Card 4: Runway & Total Interest
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
            {
                col.Item().Text(isAr ? "شهور الأمان (Runway)" : "Emergency Runway").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text(isAr ? $"{report.Affordability.EmergencyRunwayMonths:0.0} شهور" : $"{report.Affordability.EmergencyRunwayMonths:0.0} Months").FontSize(12).Bold();
                col.Item().PaddingTop(4).Text(isAr ? "إجمالي الفائدة المدفوعة" : "Total Interest Paid").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{report.AmortizationSchedule.TotalInterest:N0} {currency}").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
            });
        });
    }

    private static void ComposeStressTestTable(IContainer container, ComprehensiveLoanReportDto report, bool isAr)
    {
        container.Column(col =>
        {
            col.Item().Text(isAr ? "نتائج محاكاة الصدمات المالية (Stress Testing)" : "Financial Stress Testing Simulation Shocks").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);

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
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "السيناريو" : "Scenario").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "الحالة" : "Status").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "التأثير والأثر على المدخرات" : "Shock Impact & Reserve Status").Bold().FontSize(8.5f);
                });

                foreach (var s in report.StressTest.Scenarios)
                {
                    var statusColor = s.IsSurviving ? Colors.Green.Darken2 : Colors.Red.Darken2;
                    var statusText = s.IsSurviving 
                        ? (isAr ? "نجاح" : "Surpassed") 
                        : (isAr ? "تعثر" : "Defaulted");

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(s.ScenarioName).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(statusText).Bold().FontColor(statusColor).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(s.ImpactDescription).FontSize(8.5f);
                }
            });
        });
    }

    private static void ComposeMonteCarloSection(IContainer container, ComprehensiveLoanReportDto report, bool isAr, string currency)
    {
        var mc = report.MonteCarlo;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text(isAr 
                ? $"محاكاة مونت كارلو العشوائية ({mc.TotalIterations:N0} مسار محاكى)" 
                : $"Monte Carlo Stochastic Simulation ({mc.TotalIterations:N0} Paths)").FontSize(10.5f).Bold().FontColor(Colors.Blue.Darken3);

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(isAr ? $"احتمالية التعثر: {mc.DefaultProbabilityPercentage:0.0}%" : $"Default Probability: {mc.DefaultProbabilityPercentage:0.0}%")
                   .Bold().FontColor(mc.DefaultProbabilityPercentage > 10 ? Colors.Red.Darken2 : Colors.Green.Darken2);

                var peakText = mc.PeakRiskMonth.HasValue 
                    ? (isAr ? $"الشهر {mc.PeakRiskMonth}" : $"Month {mc.PeakRiskMonth}") 
                    : (isAr ? "لا يوجد" : "None");
                row.RelativeItem().Text(isAr ? $"أخطر شهر متوقع: {peakText}" : $"Peak Risk Month: {peakText}")
                   .FontSize(9).FontColor(Colors.Grey.Darken3);

                row.RelativeItem().Text(isAr ? $"متوسط الرصيد النهائي: {mc.MedianEndingSavings:N0} {currency}" : $"Median Ending Savings: {mc.MedianEndingSavings:N0} {currency}")
                   .FontSize(9).FontColor(Colors.Grey.Darken3);
            });
        });
    }

    private static void ComposeAlternativesTable(IContainer container, ComprehensiveLoanReportDto report, bool isAr, string currency)
    {
        container.Column(col =>
        {
            col.Item().Text(isAr ? "مقارنة البدائل المتاحة في السوق المصري" : "Egyptian Market Alternative Products Comparison").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);

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
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "الجهة والمنتج" : "Provider & Product").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "القسط الشهري" : "Monthly Installment").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "الفائدة الفعلية" : "Effective APR").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "مقارنة بالقرض الحالي" : "Comparison vs Current").Bold().FontSize(8.5f);
                });

                foreach (var alt in report.Recommendation.TopAlternatives.Take(4))
                {
                    string diffText;
                    if (alt.IsCheaperThanCurrent)
                    {
                        diffText = isAr ? $"أرخص بـ {alt.CostDifferenceComparedToCurrent:N0} {currency}" : $"Saves {alt.CostDifferenceComparedToCurrent:N0} {currency}";
                    }
                    else
                    {
                        diffText = isAr ? $"أغلى بـ {alt.CostDifferenceComparedToCurrent:N0} {currency}" : $"+{alt.CostDifferenceComparedToCurrent:N0} {currency} more";
                    }
                    var diffColor = alt.IsCheaperThanCurrent ? Colors.Green.Darken2 : Colors.Grey.Darken2;

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{alt.Product.ProviderName}").FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{alt.MonthlyInstallment:N0} {currency}").FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{alt.EffectiveAprPercentage:0.0}%").FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(diffText).Bold().FontColor(diffColor).FontSize(8.5f);
                }
            });
        });
    }

    private static void ComposeRepaymentStrategiesTable(IContainer container, ComprehensiveLoanReportDto report, bool isAr, string currency)
    {
        container.Column(col =>
        {
            col.Item().Text(isAr ? "خيارات واستراتيجيات السداد البديلة (سداد معجل، تسريع، تأجيل)" : "Alternative Repayment & Payoff Strategies").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3.2f); // Strategy Name
                    columns.RelativeColumn(1.6f); // Monthly Payment
                    columns.RelativeColumn(1.2f); // Payoff Mos
                    columns.RelativeColumn(1.7f); // Early Fee
                    columns.RelativeColumn(2.0f); // Net Benefit
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "الخطة" : "Strategy").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "القسط" : "Monthly").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "المدة" : "Term").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "غرامة السداد" : "Early Fee").Bold().FontSize(8.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(isAr ? "صافي الأثر" : "Net Benefit").Bold().FontSize(8.5f);
                });

                foreach (var strat in report.Recommendation.Strategies)
                {
                    var benefitColor = strat.NetFinancialBenefit > 0 ? Colors.Green.Darken2 : (strat.NetFinancialBenefit < 0 ? Colors.Red.Darken2 : Colors.Grey.Darken2);
                    var benefitText = strat.NetFinancialBenefit > 0
                        ? (isAr ? $"+{strat.NetFinancialBenefit:N0} {currency}" : $"+{strat.NetFinancialBenefit:N0} {currency}")
                        : (strat.NetFinancialBenefit < 0 
                            ? (isAr ? $"{strat.NetFinancialBenefit:N0} {currency}" : $"{strat.NetFinancialBenefit:N0} {currency}")
                            : (isAr ? "الأساس" : "Baseline"));

                    var termText = isAr ? $"{strat.TotalMonthsToPayoff} شهر" : $"{strat.TotalMonthsToPayoff} Mos";
                    var feeText = strat.EarlySettlementFeePaid > 0 ? $"{strat.EarlySettlementFeePaid:N0} {currency}" : (isAr ? "لا يوجد" : "None");

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(strat.StrategyName).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{strat.MonthlyPayment:N0} {currency}").FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(termText).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(feeText).FontSize(8.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(benefitText).Bold().FontColor(benefitColor).FontSize(8.5f);
                }
            });
        });
    }

    private static void ComposeAdviceSection(IContainer container, ComprehensiveLoanReportDto report, bool isAr)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(col =>
        {
            col.Item().Text(isAr ? "نصائح عملية هامة" : "Key Actionable Advice & Recommendations").FontSize(10).Bold().FontColor(Colors.Blue.Darken3);
            foreach (var advice in report.Recommendation.KeyActionableAdvice)
            {
                col.Item().PaddingTop(2).Text($"• {advice}").FontSize(8.5f);
            }
        });
    }

    private static void ComposeAiNarrativeSection(IContainer container, LoanNarrativeAdvice ai, bool isAr)
    {
        container.Border(1).BorderColor(Colors.Blue.Lighten3).Background(Colors.Blue.Lighten5).Padding(10).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text(isAr ? "موجز المستشار المالي الذكي (AI Financial Advisor)" : "AI Financial Advisor Briefing")
                    .FontSize(11).Bold().FontColor(Colors.Blue.Darken4);
                r.RelativeItem().AlignRight().Text($"[{ai.ModelUsed}]")
                    .FontSize(7.5f).FontColor(Colors.Blue.Darken2);
            });

            if (!string.IsNullOrWhiteSpace(ai.ExecutiveSummary))
            {
                col.Item().PaddingTop(4).Text(ai.ExecutiveSummary).FontSize(8.5f).LineHeight(1.25f);
            }

            if (ai.KeyStrengths.Count > 0)
            {
                col.Item().PaddingTop(4).Text(isAr ? "أبرز عوامل الأمان المالي:" : "Key Financial Strengths:").Bold().FontSize(8.5f).FontColor(Colors.Green.Darken3);
                foreach (var s in ai.KeyStrengths)
                {
                    col.Item().PaddingTop(1).Text($"✔ {s}").FontSize(8f);
                }
            }

            if (ai.VulnerabilitiesAndWarnings.Count > 0)
            {
                col.Item().PaddingTop(4).Text(isAr ? "المخاطر ونقاط الضعف الحرجة:" : "Vulnerabilities & Critical Warnings:").Bold().FontSize(8.5f).FontColor(Colors.Red.Darken3);
                foreach (var w in ai.VulnerabilitiesAndWarnings)
                {
                    col.Item().PaddingTop(1).Text($"⚠ {w}").FontSize(8f);
                }
            }

            if (ai.ActionPlanSteps.Count > 0)
            {
                col.Item().PaddingTop(4).Text(isAr ? "خطة العمل التنفيذية الموصى بها:" : "Recommended Action Plan:").Bold().FontSize(8.5f).FontColor(Colors.Blue.Darken3);
                for (int i = 0; i < ai.ActionPlanSteps.Count; i++)
                {
                    col.Item().PaddingTop(1).Text($"{i + 1}. {ai.ActionPlanSteps[i]}").FontSize(8f);
                }
            }

            if (!string.IsNullOrWhiteSpace(ai.EgyptianMarketInsight))
            {
                col.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Orange.Lighten2).Background(Colors.Orange.Lighten5).Padding(4).Column(mc =>
                {
                    mc.Item().Text(isAr ? "💡 رؤية خاصة بالسوق المصرفي المصري:" : "💡 Egyptian Market Nuance:").Bold().FontSize(8f).FontColor(Colors.Orange.Darken4);
                    mc.Item().Text(ai.EgyptianMarketInsight).FontSize(8f);
                });
            }
        });
    }

    private static void ComposeFooter(IContainer container, bool isAr)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem(3).Text(isAr 
                    ? "إخلاء مسؤولية: هذا التقرير أداة استرشادية مساعدة للقرار المالي ولا يعد استشارة رسمية ملزمة. البيانات المالية سرية ومحمية."
                    : "Disclaimer: This report is an advisory decision-support tool and does not constitute formal binding financial advice. Financial data is confidential.")
                   .FontSize(7.5f).FontColor(Colors.Grey.Medium);
                row.RelativeItem(1).AlignRight().Text(x =>
                {
                    if (isAr)
                    {
                        x.Span("صفحة ");
                        x.CurrentPageNumber();
                        x.Span(" من ");
                        x.TotalPages();
                    }
                    else
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    }
                });
            });
        });
    }
}
