using System.ComponentModel.DataAnnotations;
using System.IO;

namespace CarelessWhisperV2.Models;

public enum LlmProvider
{
    OpenRouter,
    Ollama
}

public class ApplicationSettings : IValidatableObject
{
    public string Theme { get; set; } = "Dark";
    public bool AutoStartWithWindows { get; set; } = false;
    public HotkeySettings Hotkeys { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public WhisperSettings Whisper { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public LlmProvider SelectedLlmProvider { get; set; } = LlmProvider.OpenRouter; // NEW
    public OpenRouterSettings OpenRouter { get; set; } = new(); // NEW
    public OllamaSettings Ollama { get; set; } = new(); // NEW
    public AudioNotificationSettings AudioNotification { get; set; } = new(); // NEW
    public VisionSettings Vision { get; set; } = new(); // NEW V3.6.3

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
            
        if (Ollama == null)
            yield return new ValidationResult("Ollama configuration is required");
    }
}

public class HotkeySettings
{
    public string PushToTalkKey { get; set; } = "F1";
    public string LlmPromptKey { get; set; } = "Shift+F2"; // NEW
    public bool RequireModifiers { get; set; } = false;
    public List<string> Modifiers { get; set; } = new();
}

public class AudioSettings
{
    public string PreferredDeviceId { get; set; } = "";
    public int SampleRate { get; set; } = 16000;
    public int BufferSize { get; set; } = 1024;
}

public class WhisperSettings
{
    public string ModelSize { get; set; } = "Base";
    public bool EnableGpuAcceleration { get; set; } = true;
    public string Language { get; set; } = "auto";
}

public class LoggingSettings
{
    public bool EnableTranscriptionLogging { get; set; } = true;
    public bool SaveAudioFiles { get; set; } = false;
    public int LogRetentionDays { get; set; } = 30;
}

public class AudioNotificationSettings : IValidatableObject
{
    public bool EnableNotifications { get; set; } = false;
    public string AudioFilePath { get; set; } = "";
    public double Volume { get; set; } = 0.5; // 0.0 to 1.0
    public bool PlayOnSpeechToText { get; set; } = true;
    public bool PlayOnLlmResponse { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EnableNotifications && string.IsNullOrWhiteSpace(AudioFilePath))
            yield return new ValidationResult("Audio file path is required when notifications are enabled");

        if (Volume < 0.0 || Volume > 1.0)
            yield return new ValidationResult("Volume must be between 0.0 and 1.0");

        if (EnableNotifications && !string.IsNullOrWhiteSpace(AudioFilePath))
        {
            var extension = Path.GetExtension(AudioFilePath)?.ToLowerInvariant();
            if (extension != ".wav" && extension != ".mp3")
                yield return new ValidationResult("Audio file must be .wav or .mp3 format");
        }
    }
}

public class VisionSettings : IValidatableObject
{
    public string SystemPrompt { get; set; } = "Describe the image in a single line paragraph";
    public int MaxTokens { get; set; } = 1000;
    public int ImageQuality { get; set; } = 85; // JPEG quality 1-100
    public bool EnableVisionCapture { get; set; } = true;
    public string SelectedVisionModel { get; set; } = ""; // Will use current LLM provider's model
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(SystemPrompt))
            yield return new ValidationResult("Vision system prompt cannot be empty");
            
        if (MaxTokens < 100 || MaxTokens > 4000)
            yield return new ValidationResult("Max tokens must be between 100 and 4000");
            
        if (ImageQuality < 1 || ImageQuality > 100)
            yield return new ValidationResult("Image quality must be between 1 and 100");
    }
}
