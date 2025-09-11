using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarelessWhisperV2.Services.TTS;

public interface ITTSService : IDisposable
{
    /// <summary>
    /// Gets all available SAPI voices on the system
    /// </summary>
    Task<IEnumerable<VoiceInfo>> GetAvailableVoicesAsync();
    
    /// <summary>
    /// Speaks the provided text using the configured voice
    /// </summary>
    Task SpeakTextAsync(string text);
    
    /// <summary>
    /// Stops any currently playing speech immediately
    /// </summary>
    Task StopSpeechAsync();
    
    /// <summary>
    /// Sets the voice to use for speech synthesis
    /// </summary>
    Task SetVoiceAsync(string voiceName);
    
    /// <summary>
    /// Sets the speech rate (0-10, 5 is normal)
    /// </summary>
    Task SetRateAsync(int rate);
    
    /// <summary>
    /// Sets the speech volume (0-100)
    /// </summary>
    Task SetVolumeAsync(int volume);
    
    /// <summary>
    /// Gets the currently selected voice name
    /// </summary>
    string CurrentVoiceName { get; }
    
    /// <summary>
    /// Gets whether TTS is currently speaking
    /// </summary>
    bool IsSpeaking { get; }
    
    /// <summary>
    /// Event fired when speech synthesis completes
    /// </summary>
    event EventHandler<TTSCompletedEventArgs>? SpeechCompleted;
    
    /// <summary>
    /// Event fired when speech synthesis encounters an error
    /// </summary>
    event EventHandler<TTSErrorEventArgs>? SpeechError;
}

public class VoiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class TTSCompletedEventArgs : EventArgs
{
    public string Text { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public class TTSErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Constants for standardized 3-word error messages
/// </summary>
public static class TTSErrorMessages
{
    public const string EmptyClipboard = "Nothing to speak";
    public const string ServiceFailed = "Speech service failed";
    public const string InvalidContent = "Invalid clipboard content";
    public const string VoiceNotFound = "Voice not found";
    public const string InitializationFailed = "TTS initialization failed";
}
