namespace CarelessWhisperV2.Models;

public class TtsConfiguration
{
    public bool EnableTts { get; set; } = true;
    public string SelectedVoice { get; set; } = "expr-voice-2-f";
    public float SpeechSpeed { get; set; } = 1.0f;
    public int MaxTextLength { get; set; } = 5000;
    public string PythonExecutable { get; set; } = "";
    public bool UseFallbackSapi { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    
    // Audio settings
    public float Volume { get; set; } = 1.0f;
    public bool EnableAudioNotifications { get; set; } = true;
    
    // Performance settings
    public bool EnableCaching { get; set; } = false; // Future feature
    public int MaxCacheSize { get; set; } = 100;     // Future feature
    
    // Python environment settings
    public string PythonHome { get; set; } = "";
    public string ScriptsDirectory { get; set; } = "";
    public bool UseEmbeddedPython { get; set; } = true;
}
