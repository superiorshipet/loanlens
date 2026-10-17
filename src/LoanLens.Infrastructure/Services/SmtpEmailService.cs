using System.Net;
using System.Net.Mail;
using LoanLens.Application.Interfaces;
using LoanLens.Domain.Localization;
using LoanLens.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanLens.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options?.Value ?? new SmtpOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string recipientName,
        string resetToken,
        string language = "en",
        CancellationToken ct = default)
    {
        var isAr = LoanLensLanguage.IsArabic(language);
        var subject = isAr ? "إعادة تعيين كلمة المرور - منصة LoanLens" : "Reset Your Password - LoanLens";
        var resetUrl = $"{_options.ResetUrlBase.TrimEnd('/')}?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(resetToken)}";

        string body;
        if (isAr)
        {
            body = $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .card {{ max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; padding: 36px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); }}
        .logo {{ font-size: 24px; font-weight: 800; color: #1d4ed8; letter-spacing: -0.5px; margin-bottom: 24px; text-align: center; }}
        h1 {{ font-size: 20px; font-weight: 700; color: #0f172a; margin-top: 0; margin-bottom: 16px; }}
        p {{ font-size: 15px; line-height: 1.6; color: #475569; margin: 12px 0; }}
        .token-box {{ background-color: #f1f5f9; border: 1px dashed #cbd5e1; border-radius: 8px; padding: 14px 18px; margin: 24px 0; text-align: center; font-family: monospace; font-size: 16px; font-weight: 700; color: #0f172a; word-break: break-all; }}
        .btn {{ display: block; width: fit-content; margin: 28px auto; background-color: #1d4ed8; color: #ffffff !important; text-decoration: none; padding: 14px 32px; border-radius: 9999px; font-weight: 600; font-size: 15px; text-align: center; }}
        .footer {{ margin-top: 32px; padding-top: 20px; border-top: 1px solid #f1f5f9; font-size: 12px; color: #94a3b8; text-align: center; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='logo'>LoanLens ⚖️</div>
        <h1>مرحباً {recipientName}،</h1>
        <p>تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك في منصة <strong>LoanLens</strong> للاستشارات المالية والمصرفية.</p>
        <p>رمز التحقق المؤقت لإعادة تعيين كلمة المرور هو:</p>
        <div class='token-box'>{resetToken}</div>
        <p>يمكنك أيضاً الضغط على الزر أدناه لإتمام إعادة التعيين فوراً:</p>
        <a href='{resetUrl}' class='btn'>إعادة تعيين كلمة المرور</a>
        <p style='color: #ef4444; font-size: 13px;'>⚠️ تنبيه أمني: هذا الرابط والرمز صالحان للاستخدام لمدة <strong>15 دقيقة فقط</strong>.</p>
        <p>إذا لم تكن قد طلبت إعادة تعيين كلمة المرور، يمكنك تجاهل هذا البريد الإلكتروني بأمان.</p>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} LoanLens Platform. جميع الحقوق محفوظة.
        </div>
    </div>
</body>
</html>";
        }
        else
        {
            body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .card {{ max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; padding: 36px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); }}
        .logo {{ font-size: 24px; font-weight: 800; color: #1d4ed8; letter-spacing: -0.5px; margin-bottom: 24px; text-align: center; }}
        h1 {{ font-size: 20px; font-weight: 700; color: #0f172a; margin-top: 0; margin-bottom: 16px; }}
        p {{ font-size: 15px; line-height: 1.6; color: #475569; margin: 12px 0; }}
        .token-box {{ background-color: #f1f5f9; border: 1px dashed #cbd5e1; border-radius: 8px; padding: 14px 18px; margin: 24px 0; text-align: center; font-family: monospace; font-size: 16px; font-weight: 700; color: #0f172a; word-break: break-all; }}
        .btn {{ display: block; width: fit-content; margin: 28px auto; background-color: #1d4ed8; color: #ffffff !important; text-decoration: none; padding: 14px 32px; border-radius: 9999px; font-weight: 600; font-size: 15px; text-align: center; }}
        .footer {{ margin-top: 32px; padding-top: 20px; border-top: 1px solid #f1f5f9; font-size: 12px; color: #94a3b8; text-align: center; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='logo'>LoanLens ⚖️</div>
        <h1>Hello {recipientName},</h1>
        <p>We received a request to reset your password for your <strong>LoanLens</strong> financial advisory account.</p>
        <p>Your one-time secure password reset token is:</p>
        <div class='token-box'>{resetToken}</div>
        <p>You can also click the button below to directly reset your password:</p>
        <a href='{resetUrl}' class='btn'>Reset Password</a>
        <p style='color: #ef4444; font-size: 13px;'>⚠️ Security Notice: This reset token expires in <strong>15 minutes</strong>.</p>
        <p>If you did not initiate this request, no action is required and you may safely disregard this message.</p>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} LoanLens Financial Advisory. All rights reserved.
        </div>
    </div>
</body>
</html>";
        }

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendPasswordResetSuccessEmailAsync(
        string toEmail,
        string recipientName,
        string language = "en",
        CancellationToken ct = default)
    {
        var isAr = LoanLensLanguage.IsArabic(language);
        var subject = isAr ? "تم تحديث كلمة المرور بنجاح - LoanLens" : "Your Password Was Successfully Reset - LoanLens";

        string body;
        if (isAr)
        {
            body = $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .card {{ max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; padding: 36px; }}
        .logo {{ font-size: 24px; font-weight: 800; color: #16a34a; margin-bottom: 24px; text-align: center; }}
        h1 {{ font-size: 20px; font-weight: 700; color: #0f172a; margin-top: 0; }}
        p {{ font-size: 15px; line-height: 1.6; color: #475569; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='logo'>LoanLens ✓</div>
        <h1>تم تغيير كلمة المرور بنجاح</h1>
        <p>مرحباً {recipientName}، تم تحديث كلمة المرور لحسابك في منصة LoanLens بنجاح.</p>
        <p>إذا لم تكن أنت من قام بهذا الإجراء، يرجى التواصل مع فريق الدعم فوراً لحماية حسابك.</p>
    </div>
</body>
</html>";
        }
        else
        {
            body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .card {{ max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; padding: 36px; }}
        .logo {{ font-size: 24px; font-weight: 800; color: #16a34a; margin-bottom: 24px; text-align: center; }}
        h1 {{ font-size: 20px; font-weight: 700; color: #0f172a; margin-top: 0; }}
        p {{ font-size: 15px; line-height: 1.6; color: #475569; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='logo'>LoanLens ✓</div>
        <h1>Password Reset Successful</h1>
        <p>Hello {recipientName}, your password for your LoanLens account was successfully changed.</p>
        <p>If you did not perform this action, please contact our support team immediately to secure your account.</p>
    </div>
</body>
</html>";
        }

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.UserName, _options.Password)
            };

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Successfully dispatched email to {Recipient} with subject '{Subject}' via SMTP {Host}:{Port}.", toEmail, subject, _options.Host, _options.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} via SMTP {Host}:{Port}. Diagnostic fallback triggered.", toEmail, _options.Host, _options.Port);
        }
    }
}
