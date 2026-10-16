using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Domain.Enums;
using LoanLens.Infrastructure.Entities;
using LoanLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanLens.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly LoanLensDbContext _db;
    private readonly IJwtTokenGenerator _jwtGenerator;

    public AuthService(LoanLensDbContext db, IJwtTokenGenerator jwtGenerator)
    {
        _db = db;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (existingUser)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var role = await _db.Users.AnyAsync(ct) ? UserRole.User : UserRole.Admin; // First user becomes Admin

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FullName = request.FullName.Trim(),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = _jwtGenerator.GenerateToken(user.Id, user.Email, user.Role.ToString(), user.FullName);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new AuthResponseDto(user.Id, user.Email, user.FullName, user.Role.ToString(), token, expiresAt);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account has been deactivated.");
        }

        var token = _jwtGenerator.GenerateToken(user.Id, user.Email, user.Role.ToString(), user.FullName);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new AuthResponseDto(user.Id, user.Email, user.FullName, user.Role.ToString(), token, expiresAt);
    }

    public async Task<UserProfileResponseDto?> GetCurrentUserProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;

        var scenarioCount = await _db.SavedScenarios.CountAsync(s => s.UserId == userId, ct);

        return new UserProfileResponseDto(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            CreatedAt: user.CreatedAt,
            TotalSavedScenarios: scenarioCount
        );
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.ToString(),
                u.IsActive,
                u.CreatedAt,
                u.Scenarios.Count
            ))
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(ct);

        return users;
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return false;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
