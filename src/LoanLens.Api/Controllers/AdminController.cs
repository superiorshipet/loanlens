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

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var dashboard = await _authService.GetAdminDashboardAsync(ct);
        return Ok(dashboard);
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var users = await _authService.GetAllUsersAsync(search, role, isActive, ct);
        return Ok(users);
    }

    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken ct)
    {
        var user = await _authService.GetUserByIdAsync(id, ct);
        if (user is null)
            return NotFound(new { message = $"User with ID {id} was not found." });

        return Ok(user);
    }

    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken ct)
    {
        try
        {
            var updated = await _authService.UpdateUserAsync(id, request, ct);
            if (updated is null)
                return NotFound(new { message = $"User with ID {id} was not found." });

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

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

    [HttpGet("scenarios")]
    [ProducesResponseType(typeof(IReadOnlyList<SavedScenarioSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllScenarios([FromQuery] int count = 50, CancellationToken ct = default)
    {
        var scenarios = await _scenarioRepo.GetRecentScenariosAsync(count, userId: null, isAdmin: true, ct);
        return Ok(scenarios);
    }

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
