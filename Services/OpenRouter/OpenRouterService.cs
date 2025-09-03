using CarelessWhisperV2.Models;
using CarelessWhisperV2.Services.Environment;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.IO;

namespace CarelessWhisperV2.Services.OpenRouter;

public class OpenRouterService : IOpenRouterService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger<OpenRouterService> _logger;
    private readonly string _baseUrl = "https://openrouter.ai/api/v1";
    private bool _disposed;

    public OpenRouterService(IEnvironmentService environmentService, ILogger<OpenRouterService> logger)
    {
        _environmentService = environmentService;
        _logger = logger;
        
        // Create HttpClientHandler with proxy support and improved configuration
        var handler = new HttpClientHandler()
        {
            UseProxy = true,
            UseDefaultCredentials = true,
        };

        // Use system proxy settings
        if (System.Net.WebRequest.DefaultWebProxy != null)
        {
            handler.Proxy = System.Net.WebRequest.DefaultWebProxy;
            _logger.LogInformation("Using system proxy configuration");
        }

        _httpClient = new HttpClient(handler);
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CarelessWhisperV3/1.0 (Windows)");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://careless-whisper-v3.app");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "Careless Whisper V3");
        
        _logger.LogInformation("OpenRouter service initialized with proxy support");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKeyFromHeader());

    public async Task<List<OpenRouterModel>> GetAvailableModelsAsync()
    {
        try
        {
            var apiKey = await _environmentService.GetApiKeyAsync();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("No API key available for fetching models - returning fallback model");
                return GetFallbackModels();
            }

            _logger.LogInformation("Fetching available models from OpenRouter API");

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.GetAsync($"{_baseUrl}/models");
            
            _logger.LogInformation($"OpenRouter API response status: {response.StatusCode}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unauthorized response from OpenRouter API - invalid API key");
                throw new UnauthorizedAccessException("Invalid API key");
            }
            
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"OpenRouter API response length: {jsonResponse.Length} characters");
            
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                _logger.LogError("Empty response from OpenRouter API - returning fallback model");
                return GetFallbackModels();
            }

            // Log first 500 characters of response for debugging
            _logger.LogDebug($"OpenRouter API response preview: {jsonResponse.Substring(0, Math.Min(500, jsonResponse.Length))}...");

            var models = await TryDeserializeModelsAsync(jsonResponse);
            
            if (models == null || models.Count == 0)
            {
                _logger.LogWarning("No models were successfully parsed from OpenRouter API - returning fallback model");
                return GetFallbackModels();
            }

            _logger.LogInformation($"Successfully loaded {models.Count} models from OpenRouter");
            return models;
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw authorization errors so they can be handled specifically
            throw;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, $"HTTP error while fetching models from OpenRouter: {httpEx.Message}");
            throw;
        }
        catch (TaskCanceledException tcEx)
        {
            _logger.LogError(tcEx, "Request to OpenRouter API timed out");
            throw new HttpRequestException("Request timed out", tcEx);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, $"Failed to parse JSON response from OpenRouter: {jsonEx.Message}");
            _logger.LogInformation("Returning fallback model due to JSON parsing error");
            return GetFallbackModels();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while fetching models from OpenRouter: {ex.Message}");
            _logger.LogInformation("Returning fallback model due to unexpected error");
            return GetFallbackModels();
        }
    }

    private async Task<List<OpenRouterModel>?> TryDeserializeModelsAsync(string jsonResponse)
    {
        // Try different deserialization approaches to handle various API response formats
        var deserializationOptions = new[]
        {
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower },
            new JsonSerializerOptions { PropertyNamingPolicy = null }, // PascalCase
            new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true 
            }
        };

        foreach (var options in deserializationOptions)
        {
            try
            {
                _logger.LogDebug($"Attempting deserialization with policy: {options.PropertyNamingPolicy?.GetType().Name ?? "PascalCase"}");
                
                var modelsResponse = JsonSerializer.Deserialize<ModelsApiResponse>(jsonResponse, options);
                
                if (modelsResponse?.Data != null)
                {
                    var rawCount = modelsResponse.Data.Count;
                    _logger.LogInformation($"Deserialization successful - found {rawCount} raw model entries");

                    var models = modelsResponse.Data.Select(m => new OpenRouterModel
                    {
                        Id = m.Id ?? "",
                        Name = m.Name ?? m.Id ?? "",
                        Description = m.Description ?? "",
                        PricePerMToken = m.Pricing?.Prompt ?? 0,
                        ContextLength = m.ContextLength ?? 4096,
                        SupportsStreaming = true
                    }).Where(m => !string.IsNullOrEmpty(m.Id))
                    .ToList();

                    _logger.LogInformation($"After filtering, {models.Count} valid models remain");
                    
                    if (models.Count > 0)
                    {
                        // Log a few example models for debugging
                        foreach (var model in models.Take(3))
                        {
                            _logger.LogDebug($"Sample model - ID: {model.Id}, Name: {model.Name}");
                        }
                        return models;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug($"Deserialization failed with policy {options.PropertyNamingPolicy?.GetType().Name ?? "PascalCase"}: {ex.Message}");
                continue;
            }
        }

        _logger.LogWarning("All deserialization attempts failed");
        return null;
    }

    private List<OpenRouterModel> GetFallbackModels()
    {
        _logger.LogInformation("Using fallback model: anthropic/claude-sonnet-4");
        return new List<OpenRouterModel>
        {
            new OpenRouterModel
            {
                Id = "anthropic/claude-sonnet-4",
                Name = "Claude Sonnet 4 (Fallback)",
                Description = "Fallback model when API models cannot be loaded",
                PricePerMToken = 0.015m, // Approximate pricing
                ContextLength = 200000,
                SupportsStreaming = true
            }
        };
    }

    public async Task<string> ProcessPromptAsync(string userMessage, string systemPrompt, string model)
    {
        try
        {
            var apiKey = await _environmentService.GetApiKeyAsync();
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenRouter API key not configured");

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7,
                max_tokens = 1000,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            return completion?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process prompt with OpenRouter");
            throw;
        }
    }

    public async Task<IAsyncEnumerable<string>> ProcessPromptStreamAsync(string userMessage, string systemPrompt, string model)
    {
        return ProcessPromptStreamInternalAsync(userMessage, systemPrompt, model);
    }

    private async IAsyncEnumerable<string> ProcessPromptStreamInternalAsync(
        string userMessage, 
        string systemPrompt, 
        string model,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var apiKey = await _environmentService.GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenRouter API key not configured");

        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7,
            max_tokens = 1000,
            stream = true
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", httpContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                var chunk = TryParseChunk(data);
                var deltaContent = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                if (!string.IsNullOrEmpty(deltaContent))
                {
                    yield return deltaContent;
                }
            }
        }
    }

    private ChatCompletionChunk? TryParseChunk(string data)
    {
        try
        {
            return JsonSerializer.Deserialize<ChatCompletionChunk>(data, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var tempClient = new HttpClient();
            tempClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await tempClient.GetAsync($"{_baseUrl}/models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string GetApiKeyFromHeader()
    {
        return _httpClient.DefaultRequestHeaders.Authorization?.Parameter ?? "";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    // API Response Models
    private class ModelsApiResponse
    {
        public List<ModelInfo>? Data { get; set; }
    }

    private class ModelInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public PricingInfo? Pricing { get; set; }
        public int? ContextLength { get; set; }
    }

    private class PricingInfo
    {
        public decimal? Prompt { get; set; }
        public decimal? Completion { get; set; }
    }

    private class ChatCompletionResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private class ChatCompletionChunk
    {
        public List<ChunkChoice>? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class ChunkChoice
    {
        public Delta? Delta { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }

    private class Delta
    {
        public string? Content { get; set; }
    }
}
