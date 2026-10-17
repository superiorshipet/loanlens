using LoanLens.Application.DTOs;

namespace LoanLens.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<UserProfileResponseDto?> GetCurrentUserProfileAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummaryDto>> GetAllUsersAsync(string? searchTerm = null, string? role = null, bool? isActive = null, CancellationToken ct = default);
    Task<UserDetailsDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserSummaryDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto request, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default);
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken ct = default);
    Task<PasswordResetResponseDto> RequestPasswordResetAsync(ForgotPasswordRequestDto request, string language = "en", CancellationToken ct = default);
    Task<PasswordResetResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request, string language = "en", CancellationToken ct = default);
}
