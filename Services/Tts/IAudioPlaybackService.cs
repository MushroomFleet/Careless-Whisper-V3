namespace CarelessWhisperV2.Services.Tts;

public interface IAudioPlaybackService : IDisposable
{
    Task<bool> PlayAudioAsync(byte[] audioData, CancellationToken cancellationToken = default);
    Task<bool> PlayAudioFileAsync(string audioFilePath, CancellationToken cancellationToken = default);
    void StopPlayback();
    bool IsPlaying { get; }
    float Volume { get; set; }
}
