using System.Drawing;

namespace CarelessWhisperV2.Services.Vision;

public interface IVisionProcessingService
{
    /// <summary>
    /// Analyzes an image using AI vision models
    /// </summary>
    /// <param name="image">The image to analyze</param>
    /// <param name="prompt">Optional text prompt for guided analysis</param>
    /// <returns>The vision analysis result</returns>
    Task<string> AnalyzeImageAsync(Bitmap image, string? prompt = null);
    
    /// <summary>
    /// Analyzes an image with a specific prompt and returns formatted response
    /// </summary>
    /// <param name="image">The image to analyze</param>
    /// <param name="prompt">The analysis prompt</param>
    /// <param name="includePromptInResponse">Whether to include the prompt in the final response</param>
    /// <returns>The formatted analysis result</returns>
    Task<string> AnalyzeImageWithPromptAsync(Bitmap image, string prompt, bool includePromptInResponse = false);
    
    /// <summary>
    /// Gets available vision models for the current provider
    /// </summary>
    /// <returns>List of available vision model names</returns>
    Task<List<string>> GetAvailableVisionModelsAsync();
    
    /// <summary>
    /// Tests if the vision service is available and working
    /// </summary>
    /// <returns>True if the service is available</returns>
    Task<bool> IsServiceAvailableAsync();
    
    /// <summary>
    /// Captures screen area and analyzes it with AI vision
    /// </summary>
    /// <param name="prompt">Optional custom prompt for analysis</param>
    /// <returns>The vision analysis result</returns>
    Task<string> CaptureAndAnalyzeAsync(string? prompt = null);
}

public class VisionAnalysisResult
{
    public string Analysis { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
}
