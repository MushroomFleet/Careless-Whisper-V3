using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.OpenRouter;

public interface IOpenRouterService
{
    Task<List<OpenRouterModel>> GetAvailableModelsAsync();
    Task<string> ProcessPromptAsync(string userMessage, string systemPrompt, string model);
    Task<IAsyncEnumerable<string>> ProcessPromptStreamAsync(string userMessage, string systemPrompt, string model);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    bool IsConfigured { get; }
}

public class OpenRouterResponse
{
    public string Content { get; set; } = "";
    public bool IsComplete { get; set; }
    public string Error { get; set; } = "";
}
