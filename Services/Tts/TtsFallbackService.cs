using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Tts;

public class TtsFallbackService : ITtsEngine, IDisposable
{
    private readonly KittenTtsEngine _primaryEngine;
    private readonly WindowsSapiEngine _fallbackEngine;
    private readonly ILogger<TtsFallbackService> _logger;
    private bool _disposed;

    public string EngineInfo => $"Fallback TTS: {_primaryEngine.EngineInfo} → {_fallbackEngine.EngineInfo}";

    public TtsFallbackService(
        KittenTtsEngine primaryEngine,
        WindowsSapiEngine fallbackEngine,
        ILogger<TtsFallbackService> logger)
    {
        _primaryEngine = primaryEngine;
        _fallbackEngine = fallbackEngine;
        _logger = logger;
    }

    public async Task<TtsResult> GenerateAudioAsync(string text, TtsOptions options)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TtsFallbackService));

        // Try KittenTTS first
        try
        {
            var primaryAvailable = await _primaryEngine.IsAvailableAsync();
            if (primaryAvailable)
            {
                var result = await _primaryEngine.GenerateAudioAsync(text, options);
                if (result.Success)
                {
                    _logger.LogDebug($"KittenTTS succeeded in {result.GenerationTime.TotalMilliseconds}ms");
                    return result;
                }
                
                _logger.LogWarning($"KittenTTS failed: {result.ErrorMessage}");
            }
            else
            {
                _logger.LogInformation("KittenTTS not available, using fallback");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KittenTTS engine exception, falling back to SAPI");
        }

        // Fallback to Windows SAPI
        try
        {
            _logger.LogInformation("Falling back to Windows SAPI");
            var fallbackResult = await _fallbackEngine.GenerateAudioAsync(text, options);
            
            if (fallbackResult.Success)
            {
                _logger.LogInformation($"SAPI fallback succeeded in {fallbackResult.GenerationTime.TotalMilliseconds}ms");
                return fallbackResult;
            }
            else
            {
                _logger.LogError($"SAPI fallback also failed: {fallbackResult.ErrorMessage}");
            }
            
            return fallbackResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAPI fallback engine also failed");
            
            // All methods failed
            return new TtsResult
            {
                Success = false,
                ErrorMessage = $"All TTS engines failed. KittenTTS and SAPI both unavailable. Last error: {ex.Message}"
            };
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Check if either engine is available
            var primaryAvailable = await _primaryEngine.IsAvailableAsync();
            var fallbackAvailable = await _fallbackEngine.IsAvailableAsync();
            
            var anyAvailable = primaryAvailable || fallbackAvailable;
            _logger.LogDebug("TTS availability - KittenTTS: {Primary}, SAPI: {Fallback}, Any: {Any}", 
                primaryAvailable, fallbackAvailable, anyAvailable);
            
            return anyAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check TTS engines availability");
            return false;
        }
    }

    public IEnumerable<TtsVoice> GetAvailableVoices()
    {
        try
        {
            var voices = new List<TtsVoice>();
            
            // First add KittenTTS voices if available
            try
            {
                var primaryVoices = _primaryEngine.GetAvailableVoices();
                voices.AddRange(primaryVoices);
                _logger.LogDebug("Added {Count} KittenTTS voices", primaryVoices.Count());
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not get KittenTTS voices");
            }

            // Then add SAPI voices as fallback options
            try
            {
                var fallbackVoices = _fallbackEngine.GetAvailableVoices();
                voices.AddRange(fallbackVoices);
                _logger.LogDebug("Added {Count} SAPI fallback voices", fallbackVoices.Count());
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not get SAPI voices");
            }

            if (voices.Count == 0)
            {
                // Provide absolute fallback if nothing works
                voices.Add(new TtsVoice
                {
                    Id = "fallback",
                    Description = "System Default",
                    Gender = "Unknown",
                    Language = "en"
                });
            }

            return voices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get any TTS voices");
            return new List<TtsVoice>
            {
                new TtsVoice { Id = "error", Description = "TTS Error", Gender = "Unknown", Language = "en" }
            };
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _primaryEngine?.Dispose();
                _fallbackEngine?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing TTS engines");
            }
            
            _disposed = true;
            _logger.LogDebug("TtsFallbackService disposed");
        }
    }
}
