using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Audio;

public interface IAudioService : IDisposable
{
    Task StartRecordingAsync(string outputPath);
    Task StopRecordingAsync();
    bool IsRecording { get; }
    List<AudioDevice> GetAvailableMicrophones();
    event EventHandler<AudioRecordingEventArgs>? RecordingStarted;
    event EventHandler<AudioRecordingEventArgs>? RecordingStopped;
}

public class AudioRecordingEventArgs : EventArgs
{
    public string FilePath { get; set; } = "";
    public Exception? Exception { get; set; }
}
