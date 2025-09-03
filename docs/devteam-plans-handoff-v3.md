# Careless Whisper V3.0 Development Team Handoff

## Project Overview

Careless Whisper V3.0 extends the existing V2 "Speech to Paste" functionality with a powerful new "Speech-Prompt to Paste" feature powered by OpenRouter's LLM API. This document provides complete implementation guidance for the development team.

## Current Architecture (V2)

### Core Components
- **MainWindow.xaml/.cs**: Primary WPF interface with tray functionality
- **TranscriptionOrchestrator**: Coordinates all transcription workflows
- **PushToTalkManager**: Handles F1 hotkey for speech recording
- **Services Architecture**: Modular design with DI container
  - Audio service (NAudio-based recording)
  - Transcription service (Whisper.NET)
  - Clipboard service
  - Settings service (JSON-based)

### Current Workflow
1. User presses F1 (push-to-talk)
2. Audio recording starts
3. User releases F1
4. Audio sent to Whisper for transcription
5. Result copied to clipboard

## V3.0 New Features

### Dual Hotkey System
- **F1 (existing)**: Speech to Paste (unchanged)
- **Shift+F2 (new)**: Speech-Prompt to Paste with LLM enhancement

### OpenRouter Integration
- Secure API key management via .env file
- Dynamic model selection from OpenRouter API
- Configurable system prompts
- Streaming response support

## Implementation Plan

### Phase 1: Settings Infrastructure

#### 1.1 Extend ApplicationSettings Model

**File: `Models/ApplicationSettings.cs`**

Add new OpenRouter settings section:

```csharp
public class ApplicationSettings : IValidatableObject
{
    public string Theme { get; set; } = "Dark";
    public bool AutoStartWithWindows { get; set; } = false;
    public HotkeySettings Hotkeys { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public WhisperSettings Whisper { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public OpenRouterSettings OpenRouter { get; set; } = new(); // NEW

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Hotkeys == null)
            yield return new ValidationResult("Hotkeys configuration is required");
        
        if (Audio == null)
            yield return new ValidationResult("Audio configuration is required");
        
        if (Whisper == null)
            yield return new ValidationResult("Whisper configuration is required");
        
        if (Logging == null)
            yield return new ValidationResult("Logging configuration is required");
            
        if (OpenRouter == null)
            yield return new ValidationResult("OpenRouter configuration is required");
    }
}

public class HotkeySettings
{
    public string PushToTalkKey { get; set; } = "F1";
    public string LlmPromptKey { get; set; } = "Shift+F2"; // NEW
    public bool RequireModifiers { get; set; } = false;
    public List<string> Modifiers { get; set; } = new();
}
```

#### 1.2 Create OpenRouter Settings Model

**New File: `Models/OpenRouterSettings.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace CarelessWhisperV2.Models;

public class OpenRouterSettings : IValidatableObject
{
    public string ApiKey { get; set; } = "";
    public string SelectedModel { get; set; } = "openai/gpt-4o-mini";
    public string SystemPrompt { get; set; } = "You are a helpful assistant. Please provide a clear, concise response to the user's voice input.";
    public bool EnableStreaming { get; set; } = true;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public List<OpenRouterModel> AvailableModels { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            yield return new ValidationResult("OpenRouter API key is required");

        if (string.IsNullOrWhiteSpace(SelectedModel))
            yield return new ValidationResult("Model selection is required");

        if (Temperature < 0 || Temperature > 2)
            yield return new ValidationResult("Temperature must be between 0 and 2");

        if (MaxTokens < 1 || MaxTokens > 4000)
            yield return new ValidationResult("Max tokens must be between 1 and 4000");
    }
}

public class OpenRouterModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal PricePerMToken { get; set; }
    public int ContextLength { get; set; }
    public bool SupportsStreaming { get; set; } = true;
}
```

#### 1.3 Environment Service for .env Management

**New File: `Services/Environment/IEnvironmentService.cs`**

```csharp
namespace CarelessWhisperV2.Services.Environment;

public interface IEnvironmentService
{
    Task<string> GetApiKeyAsync();
    Task SaveApiKeyAsync(string apiKey);
    Task<bool> ApiKeyExistsAsync();
    Task DeleteApiKeyAsync();
}
```

**New File: `Services/Environment/EnvironmentService.cs`**

```csharp
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CarelessWhisperV2.Services.Environment;

public class EnvironmentService : IEnvironmentService
{
    private readonly ILogger<EnvironmentService> _logger;
    private readonly string _envFilePath;
    private readonly string _appDataPath;

    public EnvironmentService(ILogger<EnvironmentService> logger)
    {
        _logger = logger;
        _appDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "CarelessWhisperV3");
        _envFilePath = Path.Combine(_appDataPath, ".env");
        
        // Ensure directory exists
        Directory.CreateDirectory(_appDataPath);
    }

    public async Task<string> GetApiKeyAsync()
    {
        try
        {
            if (!File.Exists(_envFilePath))
                return "";

            var lines = await File.ReadAllLinesAsync(_envFilePath);
            var apiKeyLine = lines.FirstOrDefault(line => line.StartsWith("OPENROUTER_API_KEY="));
            
            if (apiKeyLine != null)
            {
                var encryptedKey = apiKeyLine.Substring("OPENROUTER_API_KEY=".Length);
                return DecryptString(encryptedKey);
            }

            return "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read API key from .env file");
            return "";
        }
    }

    public async Task SaveApiKeyAsync(string apiKey)
    {
        try
        {
            var encryptedKey = EncryptString(apiKey);
            var content = $"OPENROUTER_API_KEY={encryptedKey}";
            await File.WriteAllTextAsync(_envFilePath, content);
            _logger.LogInformation("API key saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save API key to .env file");
            throw;
        }
    }

    public async Task<bool> ApiKeyExistsAsync()
    {
        var apiKey = await GetApiKeyAsync();
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    public async Task DeleteApiKeyAsync()
    {
        try
        {
            if (File.Exists(_envFilePath))
            {
                File.Delete(_envFilePath);
                _logger.LogInformation("API key deleted successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete API key file");
            throw;
        }
    }

    private string EncryptString(string plainText)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            var encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt string");
            throw;
        }
    }

    private string DecryptString(string encryptedText)
    {
        try
        {
            var data = Convert.FromBase64String(encryptedText);
            var decryptedData = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt string");
            return "";
        }
    }
}
```

### Phase 2: OpenRouter Service Layer

#### 2.1 OpenRouter Service Interface

**New File: `Services/OpenRouter/IOpenRouterService.cs`**

```csharp
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
```

#### 2.2 OpenRouter Service Implementation

**New File: `Services/OpenRouter/OpenRouterService.cs`**

```csharp
using CarelessWhisperV2.Models;
using CarelessWhisperV2.Services.Environment;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;

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
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://careless-whisper-v3.app");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "Careless Whisper V3");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKeyFromHeader());

    public async Task<List<OpenRouterModel>> GetAvailableModelsAsync()
    {
        try
        {
            var apiKey = await _environmentService.GetApiKeyAsync();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("No API key available for fetching models");
                return new List<OpenRouterModel>();
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.GetAsync($"{_baseUrl}/models");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var modelsResponse = JsonSerializer.Deserialize<ModelsApiResponse>(jsonResponse, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            return modelsResponse?.Data?.Select(m => new OpenRouterModel
            {
                Id = m.Id ?? "",
                Name = m.Name ?? "",
                Description = m.Description ?? "",
                PricePerMToken = m.Pricing?.Prompt ?? 0,
                ContextLength = m.ContextLength ?? 4096,
                SupportsStreaming = true // Most OpenRouter models support streaming
            }).ToList() ?? new List<OpenRouterModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch available models from OpenRouter");
            return new List<OpenRouterModel>();
        }
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
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);
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

                try
                {
                    var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                    });

                    var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                    if (!string.IsNullOrEmpty(content))
                    {
                        yield return content;
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON chunks
                    continue;
                }
            }
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
```

### Phase 3: Enhanced Hotkey Management

#### 3.1 Update PushToTalkManager for Dual Hotkeys

**Modified File: `Services/Hotkeys/PushToTalkManager.cs`**

```csharp
using SharpHook;
using SharpHook.Native;
using Microsoft.Extensions.Logging;

namespace CarelessWhisperV2.Services.Hotkeys;

public class PushToTalkManager : IDisposable
{
    private readonly TaskPoolGlobalHook _hook;
    private readonly KeyCode _pushToTalkKey;
    private readonly KeyCode _llmPromptKey; // NEW
    private readonly HashSet<KeyCode> _activeModifiers; // NEW
    private readonly ILogger<PushToTalkManager> _logger;
    private bool _isTransmitting;
    private bool _isLlmMode; // NEW
    private readonly object _transmissionLock = new object();
    private int _hookRestartCount;
    private const int MaxRestartAttempts = 3;
    private bool _disposed;

    public event Action? TransmissionStarted;
    public event Action? TransmissionEnded;
    public event Action? LlmTransmissionStarted; // NEW
    public event Action? LlmTransmissionEnded; // NEW

    public PushToTalkManager(
        ILogger<PushToTalkManager> logger, 
        KeyCode pushToTalkKey = KeyCode.VcF1,
        KeyCode llmPromptKey = KeyCode.VcF2) // NEW
    {
        _pushToTalkKey = pushToTalkKey;
        _llmPromptKey = llmPromptKey; // NEW
        _activeModifiers = new HashSet<KeyCode>(); // NEW
        _logger = logger;
        _hook = new TaskPoolGlobalHook();
        
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        
        // Start hook on background thread for optimal performance
        Task.Run(async () => await StartHookAsync());
    }

    private async Task StartHookAsync()
    {
        try
        {
            await _hook.RunAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hook failed, attempting restart");
            await RestartHook();
        }
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        // Track modifier keys
        if (IsModifierKey(e.Data.KeyCode))
        {
            _activeModifiers.Add(e.Data.KeyCode);
            return;
        }

        // Handle F1 (Speech to Paste)
        if (e.Data.KeyCode == _pushToTalkKey)
        {
            lock (_transmissionLock)
            {
                if (!_isTransmitting)
                {
                    _isTransmitting = true;
                    _isLlmMode = false;
                    _logger.LogDebug("Push-to-talk started (Speech to Paste)");
                    TransmissionStarted?.Invoke();
                }
            }
            e.SuppressEvent = true;
        }
        // Handle Shift+F2 (Speech-Prompt to Paste)
        else if (e.Data.KeyCode == _llmPromptKey && _activeModifiers.Contains(KeyCode.VcLeftShift))
        {
            lock (_transmissionLock)
            {
                if (!_isTransmitting)
                {
                    _isTransmitting = true;
                    _isLlmMode = true;
                    _logger.LogDebug("LLM transmission started (Speech-Prompt to Paste)");
                    LlmTransmissionStarted?.Invoke();
                }
            }
            e.SuppressEvent = true;
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        // Track modifier keys
        if (IsModifierKey(e.Data.KeyCode))
        {
            _activeModifiers.Remove(e.Data.KeyCode);
            return;
        }

        // Handle key release for both modes
        if ((e.Data.KeyCode == _pushToTalkKey && !_isLlmMode) || 
            (e.Data.KeyCode == _llmPromptKey && _isLlmMode))
        {
            lock (_transmissionLock)
            {
                if (_isTransmitting)
                {
                    _isTransmitting = false;
                    
                    if (_isLlmMode)
                    {
                        _logger.LogDebug("LLM transmission ended");
                        LlmTransmissionEnded?.Invoke();
                    }
                    else
                    {
                        _logger.LogDebug("Push-to-talk ended");
                        TransmissionEnded?.Invoke();
                    }
                }
            }
            e.SuppressEvent = true;
        }
    }

    private bool IsModifierKey(KeyCode keyCode)
    {
        return keyCode == KeyCode.VcLeftShift || 
               keyCode == KeyCode.VcRightShift ||
               keyCode == KeyCode.VcLeftControl || 
               keyCode == KeyCode.VcRightControl ||
               keyCode == KeyCode.VcLeftAlt || 
               keyCode == KeyCode.VcRightAlt;
    }

    public bool IsTransmitting
    {
        get
        {
            lock (_transmissionLock)
            {
                return _isTransmitting;
            }
        }
    }

    public bool IsLlmMode // NEW
    {
        get
        {
            lock (_transmissionLock)
            {
                return _isLlmMode;
            }
        }
    }

    private async Task RestartHook()
    {
        if (_hookRestartCount < MaxRestartAttempts)
        {
            _hookRestartCount++;
            await Task.Delay(1000 * _hookRestartCount); // Exponential backoff
            
            try
            {
                await _hook.RunAsync();
                _hookRestartCount = 0; // Reset on successful restart
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hook restart attempt {Attempt} failed", _hookRestartCount);
                await RestartHook();
            }
        }
        else
        {
            _logger.LogCritical("Hook restart limit exceeded");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _hook?.Dispose();
            _disposed = true;
            _logger.LogInformation("PushToTalkManager disposed");
        }
    }
}
```

### Phase 4: Enhanced Orchestration

#### 4.1 Update TranscriptionOrchestrator for Dual Mode

**Modified File: `Services/Orchestration/TranscriptionOrchestrator.cs`**

Add new dependencies and event handlers:

```csharp
// Add to constructor dependencies
private readonly IOpenRouterService _openRouterService;

// Add to constructor
public TranscriptionOrchestrator(
    PushToTalkManager hotkeyManager,
    IAudioService audioService,
    ITranscriptionService transcriptionService,
    IClipboardService clipboardService,
    ITranscriptionLogger transcriptionLogger,
    ISettingsService settingsService,
    IOpenRouterService openRouterService, // NEW
    ILogger<TranscriptionOrchestrator> logger)
{
    _hotkeyManager = hotkeyManager;
    _audioService = audioService;
    _transcriptionService = transcriptionService;
    _clipboardService = clipboardService;
    _transcriptionLogger = transcriptionLogger;
    _settingsService = settingsService;
    _openRouterService = openRouterService; // NEW
    _logger = logger;

    _hotkeyManager.TransmissionStarted += OnTransmissionStarted;
    _hotkeyManager.TransmissionEnded += OnTransmissionEnded;
    _hotkeyManager.LlmTransmissionStarted += OnLlmTransmissionStarted; // NEW
    _hotkeyManager.LlmTransmissionEnded += OnLlmTransmissionEnded; // NEW
    
    // Load settings
    _ = Task.Run(LoadSettingsAsync);
}

// Add new event handlers
private async void OnLlmTransmissionStarted()
{
    try
    {
        _currentRecordingPath = GenerateRecordingPath();
        await _audioService.StartRecordingAsync(_currentRecordingPath);
        _logger.LogInformation("LLM recording started: {Path}", _currentRecordingPath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to start LLM recording");
        TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
        {
            Exception = ex,
            Message = "Failed to start LLM recording"
        });
    }
}

private async void OnLlmTransmissionEnded()
{
    try
    {
        await _audioService.StopRecordingAsync();
        _logger.LogInformation("LLM recording stopped: {Path}", _currentRecordingPath);

        // Wait for file to be fully released
        await Task.Delay(1000);

        if (File.Exists(_currentRecordingPath))
        {
            var fileInfo = new FileInfo(_currentRecordingPath);
            _logger.LogInformation("LLM audio file created: {Path}, Size: {Size} bytes", _currentRecordingPath, fileInfo.Length);
            
            // Process LLM transcription in background
            _ = Task.Run(async () => await ProcessLlmTranscriptionAsync(_currentRecordingPath));
        }
        else
        {
            _logger.LogWarning("LLM audio file not found after recording: {Path}", _currentRecordingPath);
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Message = "LLM audio file not found after recording"
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to stop LLM recording");
        TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
        {
            Exception = ex,
            Message = "Failed to stop LLM recording"
        });
    }
}

// Add new LLM processing method
private async Task ProcessLlmTranscriptionAsync(string audioFilePath)
{
    var startTime = DateTime.Now;
    
    try
    {
        _logger.LogInformation("Starting LLM transcription: {Path}", audioFilePath);
        
        // First, transcribe the audio
        var transcriptionResult = await _transcriptionService.TranscribeAsync(audioFilePath);
        
        if (string.IsNullOrWhiteSpace(transcriptionResult.FullText))
        {
            _logger.LogWarning("LLM transcription returned empty result");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Message = "No speech detected in audio for LLM processing"
            });
            return;
        }

        _logger.LogInformation("LLM transcription completed, processing with OpenRouter: {Text}", 
            transcriptionResult.FullText.Substring(0, Math.Min(50, transcriptionResult.FullText.Length)));

        // Process with OpenRouter LLM
        if (_openRouterService.IsConfigured)
        {
            var llmResponse = await _openRouterService.ProcessPromptAsync(
                transcriptionResult.FullText,
                _settings.OpenRouter.SystemPrompt,
                _settings.OpenRouter.SelectedModel);

            if (!string.IsNullOrWhiteSpace(llmResponse))
            {
                // Copy LLM response to clipboard
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.Clipboard.SetText(llmResponse);
                    _logger.LogInformation("Successfully copied LLM response to clipboard");
                });

                // Log to file if enabled
                if (_settings.Logging.EnableTranscriptionLogging)
                {
                    var transcriptionEntry = new TranscriptionEntry
                    {
                        Timestamp = startTime,
                        FullText = $"INPUT: {transcriptionResult.FullText}\n\nLLM RESPONSE: {llmResponse}",
                        Segments = transcriptionResult.Segments,
                        Language = transcriptionResult.Language,
                        Duration = DateTime.Now - startTime,
                        ModelUsed = $"Whisper:{_settings.Whisper.ModelSize} + OpenRouter:{_settings.OpenRouter.SelectedModel}",
                        AudioFilePath = _settings.Logging.SaveAudioFiles ? audioFilePath : null
                    };
                    
                    await _transcriptionLogger.LogTranscriptionAsync(transcriptionEntry);
                }

                _logger.LogInformation("LLM transcription completed: {Response}", 
                    llmResponse.Substring(0, Math.Min(100, llmResponse.Length)));

                TranscriptionCompleted?.Invoke(this, new TranscriptionCompletedEventArgs
                {
                    TranscriptionResult = new TranscriptionResult 
                    { 
                        FullText = llmResponse,
                        Language = transcriptionResult.Language,
                        Segments = transcriptionResult.Segments
                    },
                    ProcessingTime = DateTime.Now - startTime
                });
            }
            else
            {
                _logger.LogWarning("OpenRouter returned empty response");
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "LLM processing returned empty response"
                });
            }
        }
        else
        {
            _logger.LogError("OpenRouter service not configured for LLM processing");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Message = "OpenRouter API not configured. Please check settings."
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "LLM transcription processing failed: {Path}. Error: {ErrorMessage}", audioFilePath, ex.Message);
        TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
        {
            Exception = ex,
            Message = $"LLM processing failed: {ex.Message}"
        });
    }
    finally
    {
        // Clean up temporary audio file (unless settings say to keep it)
        if (!_settings.Logging.SaveAudioFiles)
        {
            try
            {
                if (File.Exists(audioFilePath))
                {
                    File.Delete(audioFilePath);
                    _logger.LogDebug("Deleted temporary LLM audio file: {Path}", audioFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary LLM file: {Path}", audioFilePath);
            }
        }
    }
}
```

### Phase 5: Enhanced Settings UI

#### 5.1 Add OpenRouter Tab to Settings Window

**Modified File: `Views/SettingsWindow.xaml`**

Add new tab after the Whisper tab:

```xml
<!-- OpenRouter Tab - NEW -->
<TabItem Header="OpenRouter">
    <StackPanel Margin="15">
        <TextBlock Text="OpenRouter LLM Configuration" FontSize="16" FontWeight="Bold" Margin="0,0,0,15"/>
        
        <TextBlock Text="API Key:" FontWeight="SemiBold" Margin="0,0,0,5"/>
        <Grid Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <PasswordBox x:Name="ApiKeyPasswordBox" 
                         Grid.Column="0"
                         Margin="0,0,10,0"/>
            
            <Button x:Name="TestApiKeyButton" 
                    Grid.Column="1"
                    Content="Test" 
                    Click="TestApiKey_Click"
                    Width="60"
                    Margin="0,0,10,0"/>
                    
            <Button x:Name="RefreshModelsButton" 
                    Grid.Column="2"
                    Content="Refresh Models" 
                    Click="RefreshModels_Click"
                    Width="100"/>
        </Grid>
        
        <TextBlock x:Name="ApiKeyStatusTextBlock" 
                   Text="" 
                   Margin="0,0,0,10" 
                   Foreground="Gray"/>
        
        <TextBlock Text="Model:" FontWeight="SemiBold" Margin="0,10,0,5"/>
        <ComboBox x:Name="ModelComboBox" 
                  SelectionChanged="ModelComboBox_SelectionChanged"
                  Margin="0,0,0,10"/>
        
        <TextBlock Text="Model Information:" FontWeight="SemiBold" Margin="0,10,0,5"/>
        <Border BorderBrush="LightGray" BorderThickness="1" Padding="10" Margin="0,0,0,15">
            <StackPanel>
                <TextBlock x:Name="ModelNameTextBlock" Text="No model selected" FontWeight="Medium"/>
                <TextBlock x:Name="ModelDescriptionTextBlock" Text="" Margin="0,5,0,0" TextWrapping="Wrap"/>
                <TextBlock x:Name="ModelPricingTextBlock" Text="" Margin="0,2,0,0"/>
                <TextBlock x:Name="ModelContextTextBlock" Text="" Margin="0,2,0,0"/>
            </StackPanel>
        </Border>
        
        <TextBlock Text="System Prompt:" FontWeight="SemiBold" Margin="0,0,0,5"/>
        <TextBox x:Name="SystemPromptTextBox" 
                 Text="You are a helpful assistant. Please provide a clear, concise response to the user's voice input."
                 Height="80"
                 TextWrapping="Wrap"
                 AcceptsReturn="True"
                 VerticalScrollBarVisibility="Auto"
                 Margin="0,0,0,15"/>
        
        <TextBlock Text="Advanced Settings:" FontWeight="SemiBold" Margin="0,0,0,10"/>
        
        <Grid Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="100"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="100"/>
            </Grid.ColumnDefinitions>
            
            <TextBlock Text="Temperature:" Grid.Column="0" VerticalAlignment="Center" Margin="0,0,10,0"/>
            <Slider x:Name="TemperatureSlider" 
                    Grid.Column="1"
                    Minimum="0" 
                    Maximum="2" 
                    Value="0.7" 
                    TickFrequency="0.1"
                    IsSnapToTickEnabled="True"
                    VerticalAlignment="Center"/>
            
            <TextBlock Text="Max Tokens:" Grid.Column="2" VerticalAlignment="Center" Margin="20,0,10,0"/>
            <TextBox x:Name="MaxTokensTextBox" 
                     Grid.Column="3"
                     Text="1000" 
                     VerticalAlignment="Center"/>
        </Grid>
        
        <CheckBox x:Name="EnableStreamingCheckBox" 
                  Content="Enable streaming responses" 
                  IsChecked="True"
                  Margin="0,0,0,10"/>
        
        <TextBlock Text="Note: You can get an API key from openrouter.ai. The Speech-Prompt feature (Shift+F2) will transcribe your speech and send it to the selected model for processing." 
                   FontStyle="Italic" 
                   Foreground="Gray" 
                   TextWrapping="Wrap"
                   Margin="0,10,0,0"/>
    </StackPanel>
</TabItem>
```

#### 5.2 Update Settings Window Code-Behind

**Modified File: `Views/SettingsWindow.xaml.cs`**

Add OpenRouter-related methods and properties:

```csharp
// Add to class properties
private readonly IOpenRouterService _openRouterService;
private readonly IEnvironmentService _environmentService;
private List<OpenRouterModel> _availableModels = new();

// Add to constructor parameters
public SettingsWindow(
    ILogger<SettingsWindow> logger,
    ISettingsService settingsService,
    IAudioService audioService,
    IOpenRouterService openRouterService, // NEW
    IEnvironmentService environmentService) // NEW
{
    InitializeComponent();
    _logger = logger;
    _settingsService = settingsService;
    _audioService = audioService;
    _openRouterService = openRouterService; // NEW
    _environmentService = environmentService; // NEW
    
    LoadSettings();
    LoadAudioDevices();
    LoadOpenRouterSettings(); // NEW
}

// Add new methods
private async void LoadOpenRouterSettings()
{
    try
    {
        // Load API key from secure storage
        var apiKey = await _environmentService.GetApiKeyAsync();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            ApiKeyPasswordBox.Password = apiKey;
            ApiKeyStatusTextBlock.Text = "API key loaded";
            ApiKeyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            
            // Load available models
            await RefreshAvailableModels();
        }
        else
        {
            ApiKeyStatusTextBlock.Text = "No API key configured";
            ApiKeyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
        }
        
        // Load OpenRouter settings
        SystemPromptTextBox.Text = _currentSettings.OpenRouter.SystemPrompt;
        TemperatureSlider.Value = _currentSettings.OpenRouter.Temperature;
        MaxTokensTextBox.Text = _currentSettings.OpenRouter.MaxTokens.ToString();
        EnableStreamingCheckBox.IsChecked = _currentSettings.OpenRouter.EnableStreaming;
        
        // Select current model
        if (!string.IsNullOrWhiteSpace(_currentSettings.OpenRouter.SelectedModel))
        {
            var selectedModel = _availableModels.FirstOrDefault(m => m.Id == _currentSettings.OpenRouter.SelectedModel);
            if (selectedModel != null)
            {
                ModelComboBox.SelectedItem = selectedModel;
                UpdateModelInfo(selectedModel);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load OpenRouter settings");
        ApiKeyStatusTextBlock.Text = "Error loading settings";
        ApiKeyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
    }
}

private async void TestApiKey_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var apiKey = ApiKeyPasswordBox.Password;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ApiKeyStatusTextBlock.Text = "Please enter an API key";
            ApiKeyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            return;
        }
        
        TestApiKeyButton.IsEnabled = false;
        ApiKeyStatusTextBlock.Text = "Testing API key...";
        ApiKeyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
        
        var isValid = await _openRouterService.ValidateApiKeyAsync(apiKey);
        
        if (isValid)
        {
            ApiKeyStatusTextBlock.Text = "API key is valid";
            ApiKeyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            
            // Save API key and refresh models
            await _environmentService.SaveApiKeyAsync(apiKey);
            await RefreshAvailableModels();
        }
        else
        {
            ApiKeyStatusTextBlock.Text = "Invalid API key";
            ApiKeyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to test API key");
        ApiKeyStatusTextBlock.Text = "Error testing API key";
        ApiKeyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
    }
    finally
    {
        TestApiKeyButton.IsEnabled = true;
    }
}

private async void RefreshModels_Click(object sender, RoutedEventArgs e)
{
    await RefreshAvailableModels();
}

private async Task RefreshAvailableModels()
{
    try
    {
        RefreshModelsButton.IsEnabled = false;
        
        _availableModels = await _openRouterService.GetAvailableModelsAsync();
        
        ModelComboBox.Items.Clear();
        foreach (var model in _availableModels)
        {
            ModelComboBox.Items.Add(model);
        }
        
        if (_availableModels.Any())
        {
            ModelComboBox.DisplayMemberPath = "Name";
            ModelComboBox.SelectedIndex = 0;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to refresh available models");
    }
    finally
    {
        RefreshModelsButton.IsEnabled = true;
    }
}

private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (ModelComboBox.SelectedItem is OpenRouterModel selectedModel)
    {
        UpdateModelInfo(selectedModel);
    }
}

private void UpdateModelInfo(OpenRouterModel model)
{
    ModelNameTextBlock.Text = model.Name;
    ModelDescriptionTextBlock.Text = model.Description;
    ModelPricingTextBlock.Text = $"Pricing: ${model.PricePerMToken:F6} per 1M tokens";
    ModelContextTextBlock.Text = $"Context Length: {model.ContextLength:N0} tokens";
}

// Update Save_Click method to include OpenRouter settings
private async void Save_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // Validate OpenRouter settings
        if (!double.TryParse(TemperatureSlider.Value.ToString(), out var temperature) || 
            temperature < 0 || temperature > 2)
        {
            MessageBox.Show("Temperature must be between 0 and 2", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (!int.TryParse(MaxTokensTextBox.Text, out var maxTokens) || 
            maxTokens < 1 || maxTokens > 4000)
        {
            MessageBox.Show("Max tokens must be between 1 and 4000", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Save API key to secure storage
        var apiKey = ApiKeyPasswordBox.Password;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            await _environmentService.SaveApiKeyAsync(apiKey);
        }
        
        // Update OpenRouter settings
        _currentSettings.OpenRouter.SystemPrompt = SystemPromptTextBox.Text;
        _currentSettings.OpenRouter.Temperature = temperature;
        _currentSettings.OpenRouter.MaxTokens = maxTokens;
        _currentSettings.OpenRouter.EnableStreaming = EnableStreamingCheckBox.IsChecked ?? true;
        
        if (ModelComboBox.SelectedItem is OpenRouterModel selectedModel)
        {
            _currentSettings.OpenRouter.SelectedModel = selectedModel.Id;
        }
        
        // Save all settings (existing code)
        await _settingsService.SaveSettingsAsync(_currentSettings);
        
        _logger.LogInformation("Settings saved successfully");
        this.DialogResult = true;
        this.Close();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save settings");
        MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### Phase 6: Dependency Registration

#### 6.1 Update Program.cs

**Modified File: `Program.cs`**

Add new service registrations:

```csharp
// Add to ConfigureServices method
services.AddSingleton<IEnvironmentService, EnvironmentService>();
services.AddSingleton<IOpenRouterService, OpenRouterService>();
```

### Phase 7: NuGet Package Requirements

Add these packages to `CarelessWhisperV2.csproj`:

```xml
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

## Implementation Notes

### Security Considerations
1. **API Key Encryption**: Uses Windows DPAPI for secure local storage
2. **Environment Isolation**: .env file stored in user's AppData folder
3. **Memory Protection**: API keys cleared from memory after use
4. **Validation**: All inputs validated before processing

### Error Handling
1. **Network Failures**: Graceful degradation with user feedback
2. **API Limits**: Rate limiting and quota handling
3. **Model Availability**: Fallback to default models
4. **Audio Processing**: Robust error recovery

### Performance Optimization
1. **Async Processing**: Non-blocking UI during LLM calls
2. **Streaming Support**: Real-time response streaming
3. **Caching**: Model list caching to reduce API calls
4. **Memory Management**: Proper disposal of HTTP resources

### Testing Strategy
1. **Unit Tests**: Service layer testing with mocks
2. **Integration Tests**: End-to-end workflow testing
3. **UI Tests**: Settings validation and user flows
4. **API Tests**: OpenRouter integration validation

## Deployment Checklist

- [ ] Update project version to 3.0.0
- [ ] Update application title and branding
- [ ] Test with multiple OpenRouter models
- [ ] Validate hotkey combinations on different systems
- [ ] Test .env file encryption/decryption
- [ ] Verify settings persistence
- [ ] Test error scenarios and recovery
- [ ] Update documentation and help files
- [ ] Create user migration guide from V2
- [ ] Package and distribute application

## User Experience Enhancements

### Status Indicators
- Visual feedback for both hotkey modes
- API connection status in tray
- Processing indicators for LLM responses

### Configuration Wizards
- First-run setup wizard for OpenRouter
- API key validation during setup
- Model recommendation based on use case

### Advanced Features (Future Versions)
- Custom model parameters per use case
- Response formatting templates
- Multi-language system prompts
- Usage analytics and cost tracking

This completes the comprehensive development handoff for Careless Whisper V3.0. The implementation provides a solid foundation for the dual-mode speech processing system with seamless OpenRouter integration.
