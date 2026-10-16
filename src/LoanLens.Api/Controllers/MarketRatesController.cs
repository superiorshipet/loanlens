using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanLens.Api.Controllers;

[ApiController]
[Route("api/v1/market-rates")]
[Route("api/market-rates")]
public class MarketRatesController : ApiControllerBase
{
    private readonly IBankRatesService _bankRatesService;

    public MarketRatesController(IBankRatesService bankRatesService)
    {
        _bankRatesService = bankRatesService;
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
}
