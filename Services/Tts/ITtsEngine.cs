using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Tts;

public interface ITtsEngine
{
    Task<TtsResult> GenerateAudioAsync(string text, TtsOptions options);
    Task<bool> IsAvailableAsync();
    IEnumerable<TtsVoice> GetAvailableVoices();
    string EngineInfo { get; }
}

public class TtsOptions
{
    public string Voice { get; set; } = "expr-voice-2-f";
    public float Speed { get; set; } = 1.0f;
    public TtsOutputFormat OutputFormat { get; set; } = TtsOutputFormat.Wav;
    public int SampleRate { get; set; } = 22050;
}

public class TtsResult
{
    public bool Success { get; set; }
    public byte[] AudioData { get; set; } = Array.Empty<byte>();
    public string ErrorMessage { get; set; } = "";
    public TimeSpan GenerationTime { get; set; }
    public string? TempFilePath { get; set; }
}

public class TtsVoice
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Language { get; set; } = "en";
}

public enum TtsOutputFormat
{
    Wav,
    Mp3
}
