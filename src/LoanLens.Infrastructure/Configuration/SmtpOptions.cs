namespace LoanLens.Infrastructure.Configuration;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = "superiorshipet@gmail.com";
    public string Password { get; set; } = "zapt glgh gsfy kvaq";
    public string FromEmail { get; set; } = "superiorshipet@gmail.com";
    public string FromName { get; set; } = "LoanLens Financial Advisory";
    public string ResetUrlBase { get; set; } = "http://localhost:5257/api/auth/reset-password";
}
