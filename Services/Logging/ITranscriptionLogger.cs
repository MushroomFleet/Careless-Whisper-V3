using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Logging;

public interface ITranscriptionLogger
{
    Task LogTranscriptionAsync(TranscriptionEntry entry);
    Task<List<TranscriptionEntry>> GetTranscriptionsAsync(DateTime? date = null);
    Task<List<TranscriptionEntry>> GetTranscriptionHistoryAsync();
    Task<List<TranscriptionEntry>> SearchTranscriptionsAsync(string searchTerm, DateTime? startDate = null, DateTime? endDate = null);
    Task<int> CleanupOldTranscriptionsAsync(int retentionDays = 30);
    Task DeleteTranscriptionAsync(string id);
    Task<int> GetTranscriptionCountAsync(DateTime? date = null);
}
