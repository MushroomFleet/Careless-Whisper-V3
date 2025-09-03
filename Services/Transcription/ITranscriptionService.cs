using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Transcription;

public interface ITranscriptionService : IDisposable
{
    Task<TranscriptionResult> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default);
    Task InitializeAsync(string modelSize = "Base");
    bool IsInitialized { get; }
    event EventHandler<TranscriptionProgressEventArgs>? ProgressChanged;
}

public class TranscriptionProgressEventArgs : EventArgs
{
    public double ProgressPercentage { get; set; }
    public string Status { get; set; } = "";
}
