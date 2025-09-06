using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Ollama;

public interface IOllamaService
{
    Task<List<OllamaModel>> GetAvailableModelsAsync(bool forceRefresh = false);
    Task<string> ProcessPromptAsync(string userMessage, string systemPrompt, string model);
    Task<IAsyncEnumerable<string>> ProcessPromptStreamAsync(string userMessage, string systemPrompt, string model);
    Task<bool> ValidateConnectionAsync(string serverUrl);
    Task<ModelCacheInfo?> GetModelsCacheInfoAsync();
    Task InvalidateModelsCacheAsync();
    Task<bool> IsConfiguredAsync();
    Task<bool> IsServerRunningAsync();
}

public class OllamaResponse
{
    public string Content { get; set; } = "";
    public bool IsComplete { get; set; }
    public string Error { get; set; } = "";
}
