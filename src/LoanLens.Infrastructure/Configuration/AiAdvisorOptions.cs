namespace LoanLens.Infrastructure.Configuration;

public class AiAdvisorOptions
{
    public const string SectionName = "AiAdvisor";

    /// <summary>
    /// LLM Provider: "Gemini" (Default, free at Google AI Studio), "Groq" (Free at console.groq.com), "OpenAI", or "Local".
    /// </summary>
    public string Provider { get; set; } = "Gemini";

    /// <summary>
    /// API Key for the chosen provider. Can also be set via environment variable LOANLENS_AI_API_KEY or GEMINI_API_KEY.
    /// If null or empty, the advisor automatically uses the built-in intelligent rule-based narrative engine.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Model identifier, e.g. "gemini-1.5-flash", "gemini-2.0-flash", or "llama-3.3-70b-versatile".
    /// </summary>
    public string ModelId { get; set; } = "gemini-1.5-flash";

    /// <summary>
    /// Base URL for the OpenAI-compatible API endpoint.
    /// </summary>
    public string? BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/openai/";

    /// <summary>
    /// Temperature for LLM output (0.2 - 0.4 recommended for factual financial advice).
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
