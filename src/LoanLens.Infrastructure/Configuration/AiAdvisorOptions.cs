namespace LoanLens.Infrastructure.Configuration;

public class AiAdvisorOptions
{
    public const string SectionName = "AiAdvisor";

    public string Provider { get; set; } = "Gemini";
    public string? GeminiApiKey { get; set; }
    public string GeminiModelId { get; set; } = "gemini-3.5-flash";
    public string? GroqApiKey { get; set; }
    public string GroqModelId { get; set; } = "openai/gpt-oss-120b";
    public double Temperature { get; set; } = 0.3;
    public int TimeoutSeconds { get; set; } = 30;
}
