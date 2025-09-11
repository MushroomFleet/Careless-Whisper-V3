using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CarelessWhisperV2.Services.TTS;

public class SAPITTSService : ITTSService
{
    private readonly SpeechSynthesizer _synthesizer;
    private readonly ILogger<SAPITTSService> _logger;
    private bool _disposed;
    private DateTime _speechStartTime;
    private string _currentText = string.Empty; // Track current text being spoken

    public event EventHandler<TTSCompletedEventArgs>? SpeechCompleted;
    public event EventHandler<TTSErrorEventArgs>? SpeechError;

    public string CurrentVoiceName => _synthesizer.Voice.Name;
    public bool IsSpeaking => _synthesizer.State == SynthesizerState.Speaking;

    public SAPITTSService(ILogger<SAPITTSService> logger)
    {
        _logger = logger;
        
        try
        {
            _synthesizer = new SpeechSynthesizer();
            
            // Set up event handlers
            _synthesizer.SpeakCompleted += OnSpeakCompleted;
            _synthesizer.SpeakStarted += OnSpeakStarted;
            
            // Set default properties
            _synthesizer.Rate = 0; // Normal speed
            _synthesizer.Volume = 100; // Full volume
            
            _logger.LogInformation("SAPI TTS Service initialized with voice: {VoiceName}", CurrentVoiceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SAPI TTS Service");
            throw new InvalidOperationException(TTSErrorMessages.InitializationFailed, ex);
        }
    }

    public async Task<IEnumerable<VoiceInfo>> GetAvailableVoicesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var voices = new List<VoiceInfo>();
                var installedVoices = _synthesizer.GetInstalledVoices();
                var defaultVoiceName = _synthesizer.Voice.Name;

                foreach (var voice in installedVoices)
                {
                    if (voice.Enabled)
                    {
                        var voiceInfo = voice.VoiceInfo;
                        voices.Add(new VoiceInfo
                        {
                            Name = voiceInfo.Name,
                            Description = voiceInfo.Description,
                            Gender = voiceInfo.Gender.ToString(),
                            Culture = voiceInfo.Culture.DisplayName,
                            IsDefault = voiceInfo.Name == defaultVoiceName
                        });
                    }
                }

                _logger.LogDebug("Found {Count} available voices", voices.Count);
                return voices.AsEnumerable();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate available voices");
                return Enumerable.Empty<VoiceInfo>();
            }
        });
    }

    public async Task SpeakTextAsync(string text)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SAPITTSService));

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Attempted to speak empty or null text");
            await SpeakErrorAsync(TTSErrorMessages.EmptyClipboard);
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                // Stop any current speech first
                if (IsSpeaking)
                {
                    _synthesizer.SpeakAsyncCancelAll();
                }

                // Track the current text being spoken
                _currentText = text;

                _logger.LogDebug("Speaking text: {TextPreview}...", 
                    text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                
                _synthesizer.SpeakAsync(text);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to speak text");
            FireSpeechError(TTSErrorMessages.ServiceFailed, ex, text);
            await SpeakErrorAsync(TTSErrorMessages.ServiceFailed);
        }
    }

    public async Task StopSpeechAsync()
    {
        if (_disposed)
            return;

        try
        {
            await Task.Run(() =>
            {
                if (IsSpeaking)
                {
                    _synthesizer.SpeakAsyncCancelAll();
                    _logger.LogDebug("Speech synthesis stopped");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop speech synthesis");
        }
    }

    public async Task SetVoiceAsync(string voiceName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SAPITTSService));

        if (string.IsNullOrWhiteSpace(voiceName))
            return;

        try
        {
            await Task.Run(() =>
            {
                _synthesizer.SelectVoice(voiceName);
                _logger.LogInformation("Voice changed to: {VoiceName}", voiceName);
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Voice not found: {VoiceName}", voiceName);
            FireSpeechError(TTSErrorMessages.VoiceNotFound, ex, voiceName);
            await SpeakErrorAsync(TTSErrorMessages.VoiceNotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set voice: {VoiceName}", voiceName);
            FireSpeechError(TTSErrorMessages.ServiceFailed, ex, voiceName);
        }
    }

    public async Task SetRateAsync(int rate)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SAPITTSService));

        // SAPI rate range is -10 to 10, clamp the input
        var clampedRate = Math.Max(-10, Math.Min(10, rate));

        try
        {
            await Task.Run(() =>
            {
                _synthesizer.Rate = clampedRate;
                _logger.LogDebug("Speech rate set to: {Rate}", clampedRate);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set speech rate: {Rate}", rate);
        }
    }

    public async Task SetVolumeAsync(int volume)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SAPITTSService));

        // SAPI volume range is 0 to 100, clamp the input
        var clampedVolume = Math.Max(0, Math.Min(100, volume));

        try
        {
            await Task.Run(() =>
            {
                _synthesizer.Volume = clampedVolume;
                _logger.LogDebug("Speech volume set to: {Volume}", clampedVolume);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set speech volume: {Volume}", volume);
        }
    }

    private void OnSpeakStarted(object? sender, SpeakStartedEventArgs e)
    {
        _speechStartTime = DateTime.UtcNow;
        _logger.LogDebug("Speech synthesis started");
    }

    private void OnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
    {
        var duration = DateTime.UtcNow - _speechStartTime;
        
        if (e.Error != null)
        {
            _logger.LogError(e.Error, "Speech synthesis completed with error");
            FireSpeechError(TTSErrorMessages.ServiceFailed, e.Error, _currentText);
        }
        else if (e.Cancelled)
        {
            _logger.LogDebug("Speech synthesis was cancelled");
        }
        else
        {
            _logger.LogDebug("Speech synthesis completed successfully in {Duration}ms", duration.TotalMilliseconds);
            SpeechCompleted?.Invoke(this, new TTSCompletedEventArgs
            {
                Text = _currentText,
                Duration = duration
            });
        }
    }

    private async Task SpeakErrorAsync(string errorMessage)
    {
        try
        {
            // Use a simple synchronous speak for error messages to avoid recursion
            _synthesizer.Speak(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to speak error message: {ErrorMessage}", errorMessage);
        }
    }

    private void FireSpeechError(string message, Exception? exception, string text)
    {
        SpeechError?.Invoke(this, new TTSErrorEventArgs
        {
            Message = message,
            Exception = exception,
            Text = text
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _synthesizer?.SpeakAsyncCancelAll();
                _synthesizer?.Dispose();
                _disposed = true;
                _logger.LogInformation("SAPI TTS Service disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing SAPI TTS Service");
            }
        }
    }
}
