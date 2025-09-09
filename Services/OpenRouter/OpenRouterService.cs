using CarelessWhisperV2.Models;
using CarelessWhisperV2.Services.Environment;
using CarelessWhisperV2.Services.Cache;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using System.IO;

namespace CarelessWhisperV2.Services.OpenRouter;

public class OpenRouterService : IOpenRouterService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IEnvironmentService _environmentService;
    private readonly IModelsCacheService _cacheService;
    private readonly ILogger<OpenRouterService> _logger;
    private readonly string _baseUrl = "https://openrouter.ai/api/v1";
    private bool _disposed;

    public OpenRouterService(IEnvironmentService environmentService, IModelsCacheService cacheService, ILogger<OpenRouterService> logger)
    {
        _environmentService = environmentService;
        _cacheService = cacheService;
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

    public async Task<bool> IsConfiguredAsync()
    {
        return await _environmentService.ApiKeyExistsAsync();
    }

    public async Task<List<OpenRouterModel>> GetAvailableModelsAsync(bool forceRefresh = false)
    {
        try
        {
            var apiKey = await _environmentService.GetApiKeyAsync();
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("No API key available for fetching models - returning fallback model");
                return GetFallbackModels();
            }

            var apiKeyHash = ModelsCacheService.HashApiKey(apiKey);
            
            // Try to get cached models first (unless forced refresh)
            if (!forceRefresh)
            {
                var cachedModels = await _cacheService.GetCachedModelsAsync(apiKeyHash);
                if (cachedModels != null && cachedModels.Count > 0)
                {
                    _logger.LogInformation($"Using cached models: {cachedModels.Count} models");
                    return cachedModels;
                }
            }

            _logger.LogInformation($"Fetching available models from OpenRouter API (force refresh: {forceRefresh})");

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

            // Log first 1000 characters of response for debugging
            _logger.LogInformation($"OpenRouter API response preview: {jsonResponse.Substring(0, Math.Min(1000, jsonResponse.Length))}...");

            var models = await TryDeserializeModelsAsync(jsonResponse);
            
            if (models == null || models.Count == 0)
            {
                _logger.LogWarning("No models were successfully parsed from OpenRouter API - returning fallback model");
                return GetFallbackModels();
            }

            // Cache the successful result
            try
            {
                await _cacheService.CacheModelsAsync(models, apiKeyHash);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Failed to cache models, but continuing with fresh data");
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
            
            // Try to fall back to cache if available
            var apiKey = await _environmentService.GetApiKeyAsync();
            if (!string.IsNullOrEmpty(apiKey))
            {
                var apiKeyHash = ModelsCacheService.HashApiKey(apiKey);
                var cachedModels = await _cacheService.GetCachedModelsAsync(apiKeyHash);
                if (cachedModels != null && cachedModels.Count > 0)
                {
                    _logger.LogInformation($"Using cached models as fallback due to network error: {cachedModels.Count} models");
                    return cachedModels;
                }
            }
            
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
        // DEBUGGING: Log the exact JSON structure we're trying to parse
        _logger.LogInformation($"DEBUG: JSON response first 2000 chars: {jsonResponse.Substring(0, Math.Min(2000, jsonResponse.Length))}");
        
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
                _logger.LogInformation($"DEBUG: Attempting deserialization with policy: {options.PropertyNamingPolicy?.GetType().Name ?? "PascalCase"}");
                
                var modelsResponse = JsonSerializer.Deserialize<ModelsApiResponse>(jsonResponse, options);
                
                if (modelsResponse?.Data != null)
                {
                    var rawCount = modelsResponse.Data.Count;
                    _logger.LogInformation($"DEBUG: Deserialization successful - found {rawCount} raw model entries");

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

                    _logger.LogInformation($"DEBUG: After filtering, {models.Count} valid models remain");
                    
                    if (models.Count > 0)
                    {
                        // Log a few example models for debugging
                        foreach (var model in models.Take(5))
                        {
                            _logger.LogInformation($"DEBUG: Sample model - ID: {model.Id}, Name: {model.Name}");
                        }
                        return models;
                    }
                    else
                    {
                        _logger.LogWarning("DEBUG: All models were filtered out - checking why...");
                        foreach (var rawModel in modelsResponse.Data.Take(3))
                        {
                            _logger.LogInformation($"DEBUG: Raw model - ID: '{rawModel.Id}', Name: '{rawModel.Name}'");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"DEBUG: modelsResponse is null or Data is null. modelsResponse: {modelsResponse != null}, Data: {modelsResponse?.Data != null}");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogInformation($"DEBUG: Deserialization failed with policy {options.PropertyNamingPolicy?.GetType().Name ?? "PascalCase"}: {ex.Message}");
                continue;
            }
        }

        _logger.LogWarning("DEBUG: All deserialization attempts failed - returning null");
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

            // Validate model parameter
            if (string.IsNullOrWhiteSpace(model))
            {
                _logger.LogWarning("Model parameter is empty, using fallback model");
                model = "anthropic/claude-sonnet-4";
            }

            _logger.LogInformation("Processing prompt with model: {Model}", model);

            // Log the exact user message being sent to LLM
            var userMessagePreview = userMessage.Length > 0 
                ? userMessage.Substring(0, Math.Min(500, userMessage.Length))
                : "[EMPTY]";
            _logger.LogInformation("OPENROUTER PROMPT - UserMessage Length: {Length}, Preview: '{Preview}'", 
                userMessage.Length, userMessagePreview);

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

            _logger.LogDebug("Sending request to OpenRouter API: {RequestBody}", jsonContent);

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenRouter API request failed. Status: {StatusCode}, Content: {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Invalid API key or insufficient permissions");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new ArgumentException($"Invalid model or request parameters. Model: {model}");
                }
                
                response.EnsureSuccessStatusCode();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Received response from OpenRouter API: {ResponseLength} characters", responseJson.Length);
            
            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var result = completion?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
            
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("OpenRouter returned empty response for model: {Model}", model);
                throw new InvalidOperationException("OpenRouter returned empty response");
            }

            _logger.LogInformation("Successfully processed prompt with model: {Model}, Response length: {Length}", 
                model, result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process prompt with OpenRouter. Model: {Model}, Error: {Error}", 
                model, ex.Message);
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

    public async Task<ModelCacheInfo?> GetModelsCacheInfoAsync()
    {
        try
        {
            var apiKey = await _environmentService.GetApiKeyAsync();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new ModelCacheInfo
                {
                    Exists = false,
                    IsValid = false,
                    ModelCount = 0
                };
            }

            var apiKeyHash = ModelsCacheService.HashApiKey(apiKey);
            return await _cacheService.GetCacheInfoAsync(apiKeyHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting models cache info");
            return null;
        }
    }

    public async Task InvalidateModelsCacheAsync()
    {
        try
        {
            var apiKey = await _environmentService.GetApiKeyAsync();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                var apiKeyHash = ModelsCacheService.HashApiKey(apiKey);
                await _cacheService.InvalidateCacheAsync(apiKeyHash);
                _logger.LogInformation("Models cache invalidated successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating models cache");
            throw;
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

    // API Response Models - Updated to match OpenRouter's actual response format
    private class ModelsApiResponse
    {
        [JsonPropertyName("data")]
        public List<ModelInfo>? Data { get; set; }
    }

    private class ModelInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("pricing")]
        public PricingInfo? Pricing { get; set; }
        
        [JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }
        
        [JsonPropertyName("architecture")]
        public ArchitectureInfo? Architecture { get; set; }
        
        [JsonPropertyName("top_provider")]
        public TopProviderInfo? TopProvider { get; set; }
    }

    private class PricingInfo
    {
        [JsonPropertyName("prompt")]
        public string? PromptString { get; set; }
        
        [JsonPropertyName("completion")]
        public string? CompletionString { get; set; }
        
        [JsonPropertyName("request")]
        public string? RequestString { get; set; }
        
        [JsonPropertyName("image")]
        public string? ImageString { get; set; }
        
        // Helper properties to convert string prices to decimals
        public decimal Prompt => decimal.TryParse(PromptString, out var result) ? result : 0m;
        public decimal Completion => decimal.TryParse(CompletionString, out var result) ? result : 0m;
    }
    
    private class ArchitectureInfo
    {
        [JsonPropertyName("modality")]
        public string? Modality { get; set; }
        
        [JsonPropertyName("tokenizer")]
        public string? Tokenizer { get; set; }
        
        [JsonPropertyName("instruct_type")]
        public string? InstructType { get; set; }
    }
    
    private class TopProviderInfo
    {
        [JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }
        
        [JsonPropertyName("max_completion_tokens")]
        public int? MaxCompletionTokens { get; set; }
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
