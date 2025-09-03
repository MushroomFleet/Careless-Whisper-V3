namespace CarelessWhisperV2.Models;

public class TranscriptionEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public string FullText { get; set; } = "";
    public List<TranscriptionSegment> Segments { get; set; } = new();
    public string Language { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public string ModelUsed { get; set; } = "";
    public string? AudioFilePath { get; set; }
    public double ProcessingDuration { get; set; }
}

public class TranscriptionSegment
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Text { get; set; } = "";
}

public class TranscriptionResult
{
    public List<TranscriptionSegment> Segments { get; set; } = new();
    public string FullText { get; set; } = "";
    public string Language { get; set; } = "";
}
