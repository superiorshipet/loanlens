namespace LoanLens.Infrastructure.Configuration;

public class ScraperOptions
{
    public const string SectionName = "Scraper";

    public bool Enabled { get; set; } = true;
    public bool RunAtStartup { get; set; } = true;
    public int IntervalHours { get; set; } = 6;
    public int TimeoutSeconds { get; set; } = 15;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 LoanLens/1.0";
    public string CbeUrl { get; set; } = "https://www.cbe.org.eg/en/monetary-policy/mpc-press-releases";
}
