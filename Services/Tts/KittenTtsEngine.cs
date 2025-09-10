using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Services.Python;
using CarelessWhisperV2.Models;
using System.Diagnostics;
using System.Text.Json;
using System.IO;

namespace CarelessWhisperV2.Services.Tts;

public class KittenTtsEngine : ITtsEngine, IDisposable
{
    private readonly PythonEnvironmentManager _pythonManager;
    private readonly ILogger<KittenTtsEngine> _logger;
    private bool _disposed;
    private List<TtsVoice>? _cachedVoices;
    private bool _initializationAttempted = false;
    private bool _initializationSucceeded = false;

    public string EngineInfo => "KittenTTS v0.1 - High-quality neural TTS";

    public KittenTtsEngine(PythonEnvironmentManager pythonManager, ILogger<KittenTtsEngine> logger)
    {
        _pythonManager = pythonManager;
        _logger = logger;
    }

    public async Task<TtsResult> GenerateAudioAsync(string text, TtsOptions options)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(KittenTtsEngine));

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (!_pythonManager.IsInitialized)
            {
                var initialized = await _pythonManager.InitializeAsync();
                if (!initialized)
                {
                    return new TtsResult
                    {
                        Success = false,
                        ErrorMessage = "Python environment not initialized"
                    };
                }
            }

            // Validate input text
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TtsResult
                {
                    Success = false,
                    ErrorMessage = "Text cannot be empty"
                };
            }

            // Limit text length for performance
            if (text.Length > 10000)
            {
                text = text.Substring(0, 10000);
                _logger.LogWarning("Text truncated to 10,000 characters for TTS generation");
            }

            // Generate temporary output file
            var outputPath = Path.GetTempFileName() + ".wav";
            
            try
            {
                // Build command arguments for Python bridge
                var bridgeScript = Path.Combine(_pythonManager.ScriptsDirectory, "kitten_tts_bridge.py");
                var escapedText = JsonSerializer.Serialize(text);
                var arguments = $"\"{bridgeScript}\" --text {escapedText} --voice \"{options.Voice}\" --speed {options.Speed:F1} --output \"{outputPath}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = _pythonManager.PythonExecutable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = _pythonManager.ScriptsDirectory
                };

                _logger.LogDebug("Starting TTS generation: {Arguments}", arguments);

                // Execute KittenTTS via Python bridge
                var result = await _pythonManager.ExecutePythonScriptAsync(startInfo);
                stopwatch.Stop();

                if (result.Success && File.Exists(outputPath))
                {
                    var audioData = await File.ReadAllBytesAsync(outputPath);
                    
                    _logger.LogInformation("TTS generation completed successfully in {Duration}ms, output size: {Size} bytes", 
                        result.Duration.TotalMilliseconds, audioData.Length);

                    return new TtsResult
                    {
                        Success = true,
                        AudioData = audioData,
                        GenerationTime = stopwatch.Elapsed,
                        TempFilePath = outputPath
                    };
                }
                else
                {
                    var errorMessage = !string.IsNullOrEmpty(result.ErrorOutput) 
                        ? result.ErrorOutput 
                        : "TTS generation failed - no output file created";
                    
                    _logger.LogError("TTS generation failed: {Error}", errorMessage);
                    
                    return new TtsResult
                    {
                        Success = false,
                        ErrorMessage = errorMessage,
                        GenerationTime = stopwatch.Elapsed
                    };
                }
            }
            finally
            {
                // Clean up temporary file if it exists
                try
                {
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {Path}", outputPath);
                }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "TTS generation failed with exception");
            
            return new TtsResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                GenerationTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Use lazy initialization - only try once per engine instance
            if (!_initializationAttempted)
            {
                _initializationAttempted = true;
                
                if (!_pythonManager.IsInitialized)
                {
                    _logger.LogDebug("Attempting lazy initialization of Python environment for KittenTTS");
                    _initializationSucceeded = await _pythonManager.InitializeAsync();
                }
                else
                {
                    _initializationSucceeded = await _pythonManager.VerifyKittenTtsAsync();
                }
                
                if (_initializationSucceeded)
                {
                    _logger.LogInformation("KittenTTS is available and ready");
                }
                else
                {
                    _logger.LogWarning("KittenTTS initialization failed - engine will be unavailable");
                }
            }
            
            return _initializationSucceeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check KittenTTS availability");
            _initializationAttempted = true;
            _initializationSucceeded = false;
            return false;
        }
    }

    public IEnumerable<TtsVoice> GetAvailableVoices()
    {
        if (_cachedVoices != null)
        {
            return _cachedVoices;
        }

        // Don't trigger Python initialization during settings load - just return fallback voices
        // Only initialize Python when explicitly requested (via RefreshTtsStatus or TTS test)
        if (!_pythonManager.IsInitialized)
        {
            _logger.LogDebug("Python not initialized, returning fallback TTS voices for settings UI");
            return GetFallbackVoices();
        }

        try
        {
            // Python is already initialized, try to get voices from Python bridge
            var voices = GetVoicesFromPythonBridge().GetAwaiter().GetResult();
            _cachedVoices = voices.ToList();
            return _cachedVoices;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get voices from Python bridge, using fallback");
            return GetFallbackVoices();
        }
    }

    private async Task<IEnumerable<TtsVoice>> GetVoicesFromPythonBridge()
    {
        if (!_pythonManager.IsInitialized)
        {
            var initialized = await _pythonManager.InitializeAsync();
            if (!initialized)
            {
                return GetFallbackVoices();
            }
        }

        try
        {
            var bridgeScript = Path.Combine(_pythonManager.ScriptsDirectory, "kitten_tts_bridge.py");
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonManager.PythonExecutable,
                Arguments = $"\"{bridgeScript}\" --list-voices",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _pythonManager.ScriptsDirectory
            };

            var result = await _pythonManager.ExecutePythonScriptAsync(startInfo);
            
            if (result.Success && !string.IsNullOrEmpty(result.StandardOutput))
            {
                var response = JsonSerializer.Deserialize<VoiceListResponse>(result.StandardOutput);
                if (response?.Success == true && response.Voices != null)
                {
                    return response.Voices.Select(v => new TtsVoice
                    {
                        Id = v.Id,
                        Description = v.Description,
                        Gender = v.Id.EndsWith("-m") ? "Male" : "Female",
                        Language = "en"
                    });
                }
            }
            
            _logger.LogWarning("Failed to get voices from bridge: {Error}", result.ErrorOutput);
            return GetFallbackVoices();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting voices from Python bridge");
            return GetFallbackVoices();
        }
    }

    private IEnumerable<TtsVoice> GetFallbackVoices()
    {
        return new[]
        {
            new TtsVoice { Id = "expr-voice-2-m", Description = "Male Voice #2 - Expressive", Gender = "Male", Language = "en" },
            new TtsVoice { Id = "expr-voice-2-f", Description = "Female Voice #2 - Expressive", Gender = "Female", Language = "en" },
            new TtsVoice { Id = "expr-voice-3-m", Description = "Male Voice #3 - Expressive", Gender = "Male", Language = "en" },
            new TtsVoice { Id = "expr-voice-3-f", Description = "Female Voice #3 - Expressive", Gender = "Female", Language = "en" },
            new TtsVoice { Id = "expr-voice-4-m", Description = "Male Voice #4 - Expressive", Gender = "Male", Language = "en" },
            new TtsVoice { Id = "expr-voice-4-f", Description = "Female Voice #4 - Expressive", Gender = "Female", Language = "en" },
            new TtsVoice { Id = "expr-voice-5-m", Description = "Male Voice #5 - Expressive", Gender = "Male", Language = "en" },
            new TtsVoice { Id = "expr-voice-5-f", Description = "Female Voice #5 - Expressive", Gender = "Female", Language = "en" }
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cachedVoices = null;
            _disposed = true;
            _logger.LogDebug("KittenTtsEngine disposed");
        }
    }

    private class VoiceListResponse
    {
        public bool Success { get; set; }
        public List<VoiceInfo>? Voices { get; set; }
    }

    private class VoiceInfo
    {
        public string Id { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
