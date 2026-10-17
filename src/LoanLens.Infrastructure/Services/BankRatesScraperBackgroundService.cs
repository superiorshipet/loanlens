using LoanLens.Application.Interfaces;
using LoanLens.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanLens.Infrastructure.Services;

public class BankRatesScraperBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ScraperOptions _options;
    private readonly ILogger<BankRatesScraperBackgroundService> _logger;

    public BankRatesScraperBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<ScraperOptions> options,
        ILogger<BankRatesScraperBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? new ScraperOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("BankRatesScraperBackgroundService is disabled in configuration.");
            return;
        }

        _logger.LogInformation("BankRatesScraperBackgroundService initialized with interval {Interval} hours.", _options.IntervalHours);

        // Initial delay to let application and DB finish startup
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        if (_options.RunAtStartup && !stoppingToken.IsCancellationRequested)
        {
            await RunScraperJobAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = TimeSpan.FromHours(Math.Max(1, _options.IntervalHours));
                _logger.LogInformation("Next bank rates scraping run scheduled in {Hours} hours.", delay.TotalHours);
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await RunScraperJobAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in periodic bank rates scraper background service.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("BankRatesScraperBackgroundService stopped.");
    }

    private async Task RunScraperJobAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Executing scheduled background scraping for Egyptian bank rates.");
            using var scope = _serviceProvider.CreateScope();
            var scraper = scope.ServiceProvider.GetRequiredService<IBankRatesScraperService>();
            var result = await scraper.ScrapeLatestRatesAsync(ct);
            _logger.LogInformation("Background scraping complete: {Count} rates updated. Status: {Message}", result.RatesUpdatedCount, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute scheduled bank rates scraping cycle.");
        }
    }
}
