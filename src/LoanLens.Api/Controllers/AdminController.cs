using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanLens.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IScenarioRepository _scenarioRepo;

    public AdminController(IAuthService authService, IScenarioRepository scenarioRepo)
    {
        _authService = authService;
        _scenarioRepo = scenarioRepo;
    }

    /// <summary>
    /// List all registered users (Admin Only)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var users = await _authService.GetAllUsersAsync(ct);
        return Ok(users);
    }

    /// <summary>
    /// Delete a user account (Admin Only)
    /// </summary>
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var success = await _authService.DeleteUserAsync(id, ct);
        if (!success)
            return NotFound(new { message = $"User with ID {id} was not found." });

        return Ok(new { message = "User deleted successfully." });
    }

    /// <summary>
    /// View all saved scenarios across all users (Admin Only)
    /// </summary>
    [HttpGet("scenarios")]
    [ProducesResponseType(typeof(IReadOnlyList<SavedScenarioSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllScenarios([FromQuery] int count = 50, CancellationToken ct = default)
    {
        var scenarios = await _scenarioRepo.GetRecentScenariosAsync(count, userId: null, isAdmin: true, ct);
        return Ok(scenarios);
    }

    /// <summary>
    /// Delete any scenario by ID (Admin Only)
    /// </summary>
    [HttpDelete("scenarios/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScenario(Guid id, CancellationToken ct)
    {
        var success = await _scenarioRepo.DeleteScenarioAsync(id, userId: null, isAdmin: true, ct);
        if (!success)
            return NotFound(new { message = $"Scenario with ID {id} was not found." });

        return Ok(new { message = "Scenario deleted successfully." });
    }
}
