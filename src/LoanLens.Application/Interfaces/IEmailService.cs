namespace LoanLens.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetToken, string language = "en", CancellationToken ct = default);
    Task SendPasswordResetSuccessEmailAsync(string toEmail, string recipientName, string language = "en", CancellationToken ct = default);
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
