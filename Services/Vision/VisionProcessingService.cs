using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Services.ScreenCapture;
using CarelessWhisperV2.Services.OpenRouter;
using CarelessWhisperV2.Services.Ollama;
using CarelessWhisperV2.Services.Clipboard;
using CarelessWhisperV2.Services.Settings;
using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Vision;

public class VisionProcessingService : IVisionProcessingService
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly ICaptureOverlayService _captureOverlayService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IOpenRouterService _openRouterService;
    private readonly IOllamaService _ollamaService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<VisionProcessingService> _logger;

    public VisionProcessingService(
        IScreenCaptureService screenCaptureService,
        ICaptureOverlayService captureOverlayService,
        IImageProcessingService imageProcessingService,
        IOpenRouterService openRouterService,
        IOllamaService ollamaService,
        IClipboardService clipboardService,
        ISettingsService settingsService,
        ILogger<VisionProcessingService> logger)
    {
        _screenCaptureService = screenCaptureService;
        _captureOverlayService = captureOverlayService;
        _imageProcessingService = imageProcessingService;
        _openRouterService = openRouterService;
        _ollamaService = ollamaService;
        _clipboardService = clipboardService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<string> AnalyzeImageAsync(Bitmap image, string? prompt = null)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting vision analysis. Image size: {Width}x{Height}", image.Width, image.Height);

            // Optimize image for vision API
            var optimizedImageBytes = _imageProcessingService.OptimizeForVisionAPI(image);
            var base64Image = _imageProcessingService.ConvertToBase64(optimizedImageBytes, ImageFormat.Png);

            _logger.LogDebug("Image optimized to {Size} bytes", optimizedImageBytes.Length);

            // Get application settings to determine which provider to use
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            
            string result;
            if (settings.SelectedLlmProvider == LlmProvider.OpenRouter)
            {
                result = await AnalyzeWithOpenRouter(base64Image, prompt);
            }
            else
            {
                result = await AnalyzeWithOllama(base64Image, prompt);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Vision analysis completed in {Duration}ms. Response length: {Length}", 
                duration.TotalMilliseconds, result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vision analysis failed");
            throw;
        }
    }

    public async Task<string> AnalyzeImageWithPromptAsync(Bitmap image, string prompt, bool includePromptInResponse = false)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        if (string.IsNullOrWhiteSpace(prompt)) throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var result = await AnalyzeImageAsync(image, prompt);
        
        if (includePromptInResponse)
        {
            return $"Prompt: {prompt}\n\nResponse: {result}";
        }
        
        return result;
    }

    public async Task<List<string>> GetAvailableVisionModelsAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            
            if (settings.SelectedLlmProvider == LlmProvider.OpenRouter)
            {
                var models = await _openRouterService.GetAvailableVisionModelsAsync();
                return models.Select(m => m.Id).ToList();
            }
            else
            {
                var models = await _ollamaService.GetAvailableModelsAsync();
                // Filter for vision models (LLaVA variants)
                return models.Where(m => m.Name.Contains("llava", StringComparison.OrdinalIgnoreCase)).Select(m => m.Name).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available vision models");
            return new List<string>();
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            
            if (settings.SelectedLlmProvider == LlmProvider.OpenRouter)
            {
                return await _openRouterService.IsConfiguredAsync();
            }
            else
            {
                var models = await _ollamaService.GetAvailableModelsAsync();
                return models.Any(m => m.Name.Contains("llava", StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check vision service availability");
            return false;
        }
    }

    /// <summary>
    /// Captures screen area and analyzes it with AI vision
    /// </summary>
    public async Task<string> CaptureAndAnalyzeAsync(string? prompt = null)
    {
        try
        {
            _logger.LogInformation("VISION DEBUG: Starting screen capture and vision analysis workflow");

            // Show capture overlay to let user select area
            var selectedArea = await _captureOverlayService.ShowCaptureOverlayAsync();
            
            _logger.LogInformation("VISION DEBUG: Overlay returned selectedArea: {Area} (null check: {IsNull})", 
                selectedArea?.ToString() ?? "NULL", selectedArea == null);
            
            if (selectedArea == null)
            {
                _logger.LogInformation("VISION DEBUG: Screen capture cancelled by user - selectedArea is null");
                return "Screen capture was cancelled.";
            }
            
            if (selectedArea.Value.Width <= 0 || selectedArea.Value.Height <= 0)
            {
                _logger.LogWarning("VISION DEBUG: Invalid selection area: {Area}", selectedArea.Value);
                return "Invalid selection area.";
            }

            _logger.LogInformation("VISION DEBUG: Valid selection received: {Area}, proceeding to capture", selectedArea.Value);

            // Capture the selected screen area
            using var screenshot = await _screenCaptureService.CaptureScreenAsync(selectedArea.Value);
            
            _logger.LogInformation("VISION DEBUG: Screenshot captured successfully. Size: {Width}x{Height}", 
                screenshot.Width, screenshot.Height);
            
            // Get vision settings for prompt
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            var effectivePrompt = prompt ?? settings.Vision.SystemPrompt;
            
            _logger.LogInformation("VISION DEBUG: Starting vision analysis with prompt: '{Prompt}'", effectivePrompt);

            // Analyze the screenshot
            var analysisResult = await AnalyzeImageAsync(screenshot, effectivePrompt);
            
            _logger.LogInformation("VISION DEBUG: Vision analysis completed successfully. Result length: {Length}", 
                analysisResult.Length);
            
            // Copy to clipboard on UI thread (required for STA thread)
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Clipboard.SetText(analysisResult);
                _logger.LogInformation("VISION DEBUG: Result copied to clipboard on UI thread");
            });
            
            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VISION DEBUG: Screen capture and vision analysis failed: {Error}", ex.Message);
            return $"Vision analysis failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Captures screen, analyzes it, and copies result to clipboard
    /// </summary>
    public async Task<string> CaptureAnalyzeAndCopyAsync(string? prompt = null)
    {
        try
        {
            var result = await CaptureAndAnalyzeAsync(prompt);
            
            // Copy result to clipboard
            await _clipboardService.SetTextAsync(result);
            _logger.LogInformation("Vision analysis result copied to clipboard");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture, analyze, and copy to clipboard");
            throw;
        }
    }

    private async Task<string> AnalyzeWithOpenRouter(string base64Image, string? prompt)
    {
        try
        {
            // Get the user's configured settings
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            var userSelectedModel = settings.OpenRouter.SelectedModel;

            _logger.LogInformation("VISION DEBUG: User's selected OpenRouter model: '{Model}'", userSelectedModel);

            // Check if the user's selected model supports vision
            var isVisionSupported = await _openRouterService.IsVisionModelAsync(userSelectedModel);
            
            if (!isVisionSupported)
            {
                var errorMessage = $"Selected model '{userSelectedModel}' does not support vision analysis. Please select a vision-capable model in settings (e.g., Claude 3, GPT-4 Vision, etc.).";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var systemPrompt = "You are a helpful AI assistant that can analyze images. Describe what you see in detail.";
            var userPrompt = prompt ?? "Analyze this image and describe what you see.";

            _logger.LogInformation("VISION DEBUG: Using user's selected model for vision: {Model}", userSelectedModel);
            
            return await _openRouterService.ProcessVisionPromptAsync(userPrompt, base64Image, systemPrompt, userSelectedModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenRouter vision analysis failed with user's selected model");
            throw new InvalidOperationException($"Failed to analyze image with OpenRouter using selected model: {ex.Message}", ex);
        }
    }

    private async Task<string> AnalyzeWithOllama(string base64Image, string? prompt)
    {
        try
        {
            var models = await _ollamaService.GetAvailableModelsAsync();
            var visionModel = models.FirstOrDefault(m => m.Name.Contains("llava", StringComparison.OrdinalIgnoreCase))?.Name ?? "llava:7b";

            var systemPrompt = "You are a helpful AI assistant that can analyze images. Describe what you see in detail.";
            var userPrompt = prompt ?? "Analyze this image and describe what you see.";

            _logger.LogDebug("Using Ollama vision model: {Model}", visionModel);
            
            // For Ollama, we would need to implement vision support
            // For now, return a placeholder indicating Ollama vision support is needed
            return await Task.FromResult("Ollama vision analysis is not yet implemented. Please use OpenRouter for vision capabilities.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama vision analysis failed");
            throw new InvalidOperationException("Failed to analyze image with Ollama", ex);
        }
    }
}
