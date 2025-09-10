using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Services.Clipboard;
using CarelessWhisperV2.Services.Settings;
using CarelessWhisperV2.Models;
using System.Windows;

namespace CarelessWhisperV2.Services.Tts;

public class TtsHotkeyHandler
{
    private readonly ITtsEngine _ttsEngine;
    private readonly IAudioPlaybackService _audioPlayback;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TtsHotkeyHandler> _logger;
    private CancellationTokenSource? _currentTtsCts;
    
    // DIAGNOSTIC: TTS handler invocation tracking
    private int _handleCtrlF1InvocationCount = 0;

    public TtsHotkeyHandler(
        ITtsEngine ttsEngine,
        IAudioPlaybackService audioPlayback,
        IClipboardService clipboardService,
        ISettingsService settingsService,
        ILogger<TtsHotkeyHandler> logger)
    {
        _ttsEngine = ttsEngine;
        _audioPlayback = audioPlayback;
        _clipboardService = clipboardService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task HandleCtrlF1Async()
    {
        try
        {
            _handleCtrlF1InvocationCount++;
            _logger.LogInformation("DIAGNOSTIC TTS: HandleCtrlF1Async called #{Count}", _handleCtrlF1InvocationCount);
            
            // Cancel any existing TTS operation
            _currentTtsCts?.Cancel();
            _currentTtsCts = new CancellationTokenSource();

            // Load current settings
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            var ttsConfig = settings.Tts;

            if (!ttsConfig.EnableTts)
            {
                _logger.LogInformation("TTS is disabled in settings");
                return;
            }

            // Check if TTS engine is available
            var isAvailable = await _ttsEngine.IsAvailableAsync();
            if (!isAvailable)
            {
                _logger.LogWarning("TTS engine is not available");
                return;
            }

            // Get clipboard text on UI thread for proper STA access
            string? clipboardText = null;
            
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            clipboardText = System.Windows.Clipboard.GetText();
                        }
                    }
                    catch (Exception clipEx)
                    {
                        _logger.LogError(clipEx, "Failed to access clipboard on UI thread");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clipboard content");
                return;
            }

            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                _logger.LogWarning("No text found in clipboard for TTS");
                return;
            }

            // Limit text length to prevent extremely long generations
            var originalLength = clipboardText.Length;
            if (clipboardText.Length > ttsConfig.MaxTextLength)
            {
                clipboardText = clipboardText.Substring(0, ttsConfig.MaxTextLength);
                _logger.LogInformation($"Truncated clipboard text from {originalLength} to {ttsConfig.MaxTextLength} characters");
            }

            _logger.LogInformation($"Starting TTS for {clipboardText.Length} characters from clipboard");

            // Generate TTS audio
            var ttsOptions = new TtsOptions
            {
                Voice = ttsConfig.SelectedVoice,
                Speed = ttsConfig.SpeechSpeed,
                OutputFormat = TtsOutputFormat.Wav
            };

            var result = await _ttsEngine.GenerateAudioAsync(clipboardText, ttsOptions);

            if (!result.Success)
            {
                _logger.LogError($"TTS generation failed: {result.ErrorMessage}");
                return;
            }

            if (result.AudioData == null || result.AudioData.Length == 0)
            {
                _logger.LogError("TTS generation returned empty audio data");
                return;
            }

            _logger.LogInformation($"TTS generation completed in {result.GenerationTime.TotalMilliseconds:F0}ms, audio size: {result.AudioData.Length} bytes");

            // Set volume and play the generated audio
            _audioPlayback.Volume = ttsConfig.Volume;
            var playbackSuccess = await _audioPlayback.PlayAudioAsync(result.AudioData, _currentTtsCts.Token);

            if (playbackSuccess)
            {
                _logger.LogInformation("TTS playback completed successfully");
            }
            else
            {
                _logger.LogWarning("TTS playback failed or was interrupted");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TTS operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TTS hotkey handler");
        }
    }

    public void CancelCurrentTts()
    {
        try
        {
            _currentTtsCts?.Cancel();
            _audioPlayback.StopPlayback();
            _logger.LogInformation("Current TTS operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling TTS operation");
        }
    }

    public bool IsProcessing => _currentTtsCts?.Token.IsCancellationRequested == false && _audioPlayback.IsPlaying;
}
