using CarelessWhisperV2.Models;
using CarelessWhisperV2.Services.Settings;
using CarelessWhisperV2.Services.Cache;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace CarelessWhisperV2.Services.Ollama;

public class OllamaService : IOllamaService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly IModelsCacheService _cacheService;
    private readonly ILogger<OllamaService> _logger;
    private bool _disposed;

    public OllamaService(ISettingsService settingsService, IModelsCacheService cacheService, ILogger<OllamaService> logger)
    {
        _settingsService = settingsService;
        _cacheService = cacheService;
        _logger = logger;
        
        var handler = new HttpClientHandler()
        {
            UseProxy = false, // Local requests don't need proxy
        };

        _httpClient = new HttpClient(handler);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CarelessWhisperV3/1.0 (Windows)");
        
        _logger.LogInformation("Ollama service initialized");
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
        return !string.IsNullOrWhiteSpace(settings.Ollama.ServerUrl) && 
               !string.IsNullOrWhiteSpace(settings.Ollama.SelectedModel);
    }

    public async Task<bool> IsServerRunningAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            var serverUrl = settings.Ollama.ServerUrl;
            
            if (string.IsNullOrWhiteSpace(serverUrl))
                return false;

            var response = await _httpClient.GetAsync($"{serverUrl.TrimEnd('/')}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama server connectivity check failed");
            return false;
        }
    }

    public async Task<List<OllamaModel>> GetAvailableModelsAsync(bool forceRefresh = false)
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            var serverUrl = settings.Ollama.ServerUrl;
            
            // First, try the API method if server URL is configured
            if (!string.IsNullOrWhiteSpace(serverUrl))
            {
                _logger.LogInformation($"Attempting to fetch models from Ollama server API: {serverUrl}");
                
                try
                {
                    var response = await _httpClient.GetAsync($"{serverUrl.TrimEnd('/')}/api/tags");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Ollama API response length: {jsonResponse.Length} characters");
                        
                        if (!string.IsNullOrWhiteSpace(jsonResponse))
                        {
                            var models = await TryDeserializeModelsAsync(jsonResponse);
                            
                            if (models != null && models.Count > 0)
                            {
                                _logger.LogInformation($"Successfully loaded {models.Count} models from Ollama API");
                                return models;
                            }
                        }
                    }
                    
                    _logger.LogWarning($"Ollama API failed or returned no models. Status: {response.StatusCode}. Falling back to CLI method.");
                }
                catch (Exception apiEx)
                {
                    _logger.LogWarning(apiEx, "Ollama API request failed. Falling back to CLI method.");
                }
            }
            
            // Fallback to CLI method using 'ollama list'
            _logger.LogInformation("Attempting to fetch models using 'ollama list' command");
            return await GetModelsFromCliAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"All methods failed to fetch Ollama models: {ex.Message}");
            return new List<OllamaModel>();
        }
    }

    private async Task<List<OllamaModel>> GetModelsFromCliAsync()
    {
        try
        {
            _logger.LogInformation("Executing 'ollama list' command to fetch models");
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputBuilder.AppendLine(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();
            
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            
            if (process.ExitCode != 0)
            {
                _logger.LogError("'ollama list' command failed with exit code {ExitCode}. Error: {Error}", 
                    process.ExitCode, error);
                throw new InvalidOperationException($"ollama list command failed: {error}");
            }
            
            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogWarning("'ollama list' command returned empty output");
                return new List<OllamaModel>();
            }
            
            _logger.LogDebug("'ollama list' output: {Output}", output);
            
            return ParseOllamaListOutput(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute 'ollama list' command: {Error}", ex.Message);
            return new List<OllamaModel>();
        }
    }

    private List<OllamaModel> ParseOllamaListOutput(string output)
    {
        var models = new List<OllamaModel>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            // Skip header line if present
            if (line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("SIZE") || 
                line.Contains("MODIFIED"))
            {
                continue;
            }
            
            // Parse model line - typical format: "modelname:tag    size    modified_date"
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                var modelName = parts[0].Trim();
                
                // Extract size if available (second column)
                long size = 0;
                if (parts.Length >= 2)
                {
                    var sizeStr = parts[1].Trim();
                    if (sizeStr.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(sizeStr.Replace("GB", ""), out var gbSize))
                            size = (long)(gbSize * 1024 * 1024 * 1024);
                    }
                    else if (sizeStr.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(sizeStr.Replace("MB", ""), out var mbSize))
                            size = (long)(mbSize * 1024 * 1024);
                    }
                }
                
                // Extract modified date if available (third column)
                DateTime modifiedAt = DateTime.MinValue;
                if (parts.Length >= 3)
                {
                    var dateStr = string.Join(" ", parts.Skip(2));
                    DateTime.TryParse(dateStr, out modifiedAt);
                }
                
                var model = new OllamaModel
                {
                    Name = modelName,
                    Model = modelName,
                    Size = size,
                    Digest = "", // Not available from CLI output
                    ModifiedAt = modifiedAt,
                    Details = null // Not available from CLI output
                };
                
                models.Add(model);
                _logger.LogDebug("Parsed model from CLI: {ModelName}, Size: {Size}", modelName, size);
            }
        }
        
        _logger.LogInformation("Successfully parsed {Count} models from 'ollama list' output", models.Count);
        return models;
    }

    private async Task<List<OllamaModel>?> TryDeserializeModelsAsync(string jsonResponse)
    {
        try
        {
            _logger.LogDebug($"Ollama models response: {jsonResponse.Substring(0, Math.Min(1000, jsonResponse.Length))}");
            
            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true 
            };
            
            var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(jsonResponse, options);
            
            if (modelsResponse?.Models != null)
            {
                var models = modelsResponse.Models.Select(m => new OllamaModel
                {
                    Name = m.Name ?? "",
                    Model = m.Model ?? m.Name ?? "",
                    Size = m.Size,
                    Digest = m.Digest ?? "",
                    ModifiedAt = m.ModifiedAt,
                    Details = m.Details != null ? new OllamaModelDetails
                    {
                        Format = m.Details.Format ?? "",
                        Family = m.Details.Family ?? "",
                        Families = m.Details.Families ?? new List<string>(),
                        ParameterSize = m.Details.ParameterSize,
                        QuantizationLevel = m.Details.QuantizationLevel
                    } : null
                }).Where(m => !string.IsNullOrEmpty(m.Name))
                .ToList();

                _logger.LogInformation($"Parsed {models.Count} valid Ollama models");
                return models;
            }
            
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Ollama models response");
            return null;
        }
    }

    public async Task<string> ProcessPromptAsync(string userMessage, string systemPrompt, string model)
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            var serverUrl = settings.Ollama.ServerUrl;
            
            if (string.IsNullOrWhiteSpace(serverUrl))
                throw new InvalidOperationException("Ollama server URL not configured");

            if (string.IsNullOrWhiteSpace(model))
            {
                _logger.LogWarning("Model parameter is empty, using configured model");
                model = settings.Ollama.SelectedModel;
            }

            _logger.LogInformation("Processing prompt with Ollama model: {Model}", model);

            // Log the exact user message being sent to LLM
            var userMessagePreview = userMessage.Length > 0 
                ? userMessage.Substring(0, Math.Min(500, userMessage.Length))
                : "[EMPTY]";
            _logger.LogInformation("OLLAMA PROMPT - UserMessage Length: {Length}, Preview: '{Preview}'", 
                userMessage.Length, userMessagePreview);

            var fullPrompt = $"{systemPrompt}\n\nUser: {userMessage}\n\nAssistant:";
            var fullPromptPreview = fullPrompt.Length > 0 
                ? fullPrompt.Substring(0, Math.Min(500, fullPrompt.Length))
                : "[EMPTY]";
            _logger.LogInformation("OLLAMA FULL PROMPT - Length: {Length}, Preview: '{Preview}'", 
                fullPrompt.Length, fullPromptPreview);

            var requestBody = new
            {
                model = model,
                prompt = fullPrompt,
                options = new
                {
                    temperature = settings.Ollama.Temperature,
                    num_predict = settings.Ollama.MaxTokens
                },
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to Ollama API: {RequestBody}", jsonContent);

            var response = await _httpClient.PostAsync($"{serverUrl.TrimEnd('/')}/api/generate", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama API request failed. Status: {StatusCode}, Content: {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                response.EnsureSuccessStatusCode();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Received response from Ollama API: {ResponseLength} characters", responseJson.Length);
            
            var completion = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var result = completion?.Response ?? "";
            
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("Ollama returned empty response for model: {Model}", model);
                throw new InvalidOperationException("Ollama returned empty response");
            }

            _logger.LogInformation("Successfully processed prompt with Ollama model: {Model}, Response length: {Length}", 
                model, result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process prompt with Ollama. Model: {Model}, Error: {Error}", 
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
        var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
        var serverUrl = settings.Ollama.ServerUrl;
        
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new InvalidOperationException("Ollama server URL not configured");

        var requestBody = new
        {
            model = model,
            prompt = $"{systemPrompt}\n\nUser: {userMessage}\n\nAssistant:",
            options = new
            {
                temperature = settings.Ollama.Temperature,
                num_predict = settings.Ollama.MaxTokens
            },
            stream = true
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync($"{serverUrl.TrimEnd('/')}/api/generate", httpContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var chunk = TryParseChunk(line);
                if (chunk != null && !string.IsNullOrEmpty(chunk.Response))
                {
                    yield return chunk.Response;
                }
                
                // Check if this is the final chunk
                if (chunk?.Done == true)
                    break;
            }
        }
    }

    private OllamaGenerateResponse? TryParseChunk(string data)
    {
        try
        {
            return JsonSerializer.Deserialize<OllamaGenerateResponse>(data, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<bool> ValidateConnectionAsync(string serverUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serverUrl))
                return false;

            var response = await _httpClient.GetAsync($"{serverUrl.TrimEnd('/')}/api/tags");
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
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            var serverUrl = settings.Ollama.ServerUrl;
            
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                return new ModelCacheInfo
                {
                    Exists = false,
                    IsValid = false,
                    ModelCount = 0
                };
            }

            // For now, return a basic cache info since Ollama caching isn't implemented yet
            return new ModelCacheInfo
            {
                Exists = false,
                IsValid = false,
                ModelCount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Ollama models cache info");
            return null;
        }
    }

    public async Task InvalidateModelsCacheAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            var serverUrl = settings.Ollama.ServerUrl;
            
            if (!string.IsNullOrWhiteSpace(serverUrl))
            {
                // For now, just log that cache invalidation was requested
                _logger.LogInformation("Ollama models cache invalidation requested (caching not yet implemented)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating Ollama models cache");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    // Custom JSON converter for flexible long conversion
    private class FlexibleLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64();
            }
            
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                    return 0;
                
                // Handle parameter size formats like "7B", "13B", "70B"
                if (stringValue.EndsWith("B", StringComparison.OrdinalIgnoreCase))
                {
                    var numberPart = stringValue.Substring(0, stringValue.Length - 1);
                    if (double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    {
                        // Convert billions to actual byte count (approximate)
                        // 1B parameters ≈ 4 bytes per parameter (for float32)
                        return (long)(value * 1_000_000_000 * 4);
                    }
                }
                
                // Handle other size formats like "1.3M", "125M"
                if (stringValue.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                {
                    var numberPart = stringValue.Substring(0, stringValue.Length - 1);
                    if (double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    {
                        // Convert millions to actual byte count
                        return (long)(value * 1_000_000 * 4);
                    }
                }
                
                // Try to parse as a regular number string
                if (long.TryParse(stringValue, out var directValue))
                {
                    return directValue;
                }
            }
            
            return 0; // Default fallback
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    // Ollama API Response Models
    private class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo>? Models { get; set; }
    }

    private class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("model")]
        public string? Model { get; set; }
        
        [JsonPropertyName("size")]
        public long Size { get; set; }
        
        [JsonPropertyName("digest")]
        public string? Digest { get; set; }
        
        [JsonPropertyName("modified_at")]
        public DateTime ModifiedAt { get; set; }
        
        [JsonPropertyName("details")]
        public OllamaModelDetailsInfo? Details { get; set; }
    }

    private class OllamaModelDetailsInfo
    {
        [JsonPropertyName("format")]
        public string? Format { get; set; }
        
        [JsonPropertyName("family")]
        public string? Family { get; set; }
        
        [JsonPropertyName("families")]
        public List<string>? Families { get; set; }
        
        [JsonPropertyName("parameter_size")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long ParameterSize { get; set; }
        
        [JsonPropertyName("quantization_level")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long QuantizationLevel { get; set; }
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
        
        [JsonPropertyName("done")]
        public bool Done { get; set; }
        
        [JsonPropertyName("model")]
        public string? Model { get; set; }
        
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("context")]
        public List<int>? Context { get; set; }
        
        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }
        
        [JsonPropertyName("load_duration")]
        public long LoadDuration { get; set; }
        
        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }
        
        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }
        
        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }
        
        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }
    }
}
