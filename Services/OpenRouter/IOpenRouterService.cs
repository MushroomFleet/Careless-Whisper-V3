using CarelessWhisperV2.Models;
using System.Drawing;

namespace CarelessWhisperV2.Services.OpenRouter;

public interface IOpenRouterService
{
    Task<bool> IsConfiguredAsync();
    Task<List<OpenRouterModel>> GetAvailableModelsAsync(bool forceRefresh = false);
    Task<string> ProcessPromptAsync(string userMessage, string systemPrompt, string model);
    Task<IAsyncEnumerable<string>> ProcessPromptStreamAsync(string userMessage, string systemPrompt, string model);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task<ModelCacheInfo?> GetModelsCacheInfoAsync();
    Task InvalidateModelsCacheAsync();
    
    // Vision model support
    Task<string> ProcessVisionPromptAsync(string userMessage, string base64Image, string systemPrompt, string model);
    Task<List<OpenRouterModel>> GetAvailableVisionModelsAsync(bool forceRefresh = false);
    Task<bool> IsVisionModelAsync(string modelId);
}

public class OpenRouterResponse
{
    public string Content { get; set; } = "";
    public bool IsComplete { get; set; }
    public string Error { get; set; } = "";
}
