using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanLens.Api.Controllers;

[ApiController]
[Route("api/market-rates")]
public class MarketRatesController : ApiControllerBase
{
    private readonly IBankRatesService _bankRatesService;
    private readonly IBankRatesScraperService _scraperService;

    public MarketRatesController(IBankRatesService bankRatesService, IBankRatesScraperService scraperService)
    {
        _bankRatesService = bankRatesService;
        _scraperService = scraperService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(BankRatesSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRates([FromQuery] string? lang, CancellationToken ct)
    {
        var language = ResolveLanguage(lang);
        var rates = await _bankRatesService.GetMarketRatesAsync(language, ct);
        return Ok(rates);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(BankRatesSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RefreshRates(CancellationToken ct)
    {
        var rates = await _bankRatesService.RefreshMarketRatesAsync(ct);
        return Ok(rates);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("scrape")]
    [ProducesResponseType(typeof(ScrapeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ScrapeRates(CancellationToken ct)
    {
        var result = await _scraperService.ScrapeLatestRatesAsync(ct);
        return Ok(result);
    }
}
