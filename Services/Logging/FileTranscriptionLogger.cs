using System.Text.Json;
using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Models;
using System.IO;

namespace CarelessWhisperV2.Services.Logging;

public class FileTranscriptionLogger : ITranscriptionLogger
{
    private readonly string _transcriptionsPath;
    private readonly ILogger<FileTranscriptionLogger> _logger;
    private readonly object _lockObject = new object();

    public FileTranscriptionLogger(ILogger<FileTranscriptionLogger> logger)
    {
        _logger = logger;
        
        var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "CarelessWhisperV2");
        _transcriptionsPath = Path.Combine(appFolder, "transcriptions");
        
        Directory.CreateDirectory(_transcriptionsPath);
    }

    public async Task LogTranscriptionAsync(TranscriptionEntry entry)
    {
        try
        {
            var date = entry.Timestamp.Date;
            var dailyFolder = Path.Combine(_transcriptionsPath, date.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dailyFolder);
            
            var transcriptionsFile = Path.Combine(dailyFolder, "transcriptions.json");
            
            lock (_lockObject)
            {
                List<TranscriptionEntry> existingTranscriptions;
                
                if (File.Exists(transcriptionsFile))
                {
                    var existingJson = File.ReadAllText(transcriptionsFile);
                    existingTranscriptions = JsonSerializer.Deserialize<List<TranscriptionEntry>>(existingJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }) ?? new List<TranscriptionEntry>();
                }
                else
                {
                    existingTranscriptions = new List<TranscriptionEntry>();
                }
                
                existingTranscriptions.Add(entry);
                
                var json = JsonSerializer.Serialize(existingTranscriptions, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                File.WriteAllText(transcriptionsFile, json);
            }
            
            _logger.LogDebug("Transcription logged: {Timestamp} - {TextLength} characters", 
                entry.Timestamp, entry.FullText.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log transcription");
            throw;
        }
    }

    public async Task<List<TranscriptionEntry>> GetTranscriptionsAsync(DateTime? date = null)
    {
        try
        {
            var targetDate = date ?? DateTime.Today;
            var dailyFolder = Path.Combine(_transcriptionsPath, targetDate.ToString("yyyy-MM-dd"));
            var transcriptionsFile = Path.Combine(dailyFolder, "transcriptions.json");
            
            if (!File.Exists(transcriptionsFile))
            {
                return new List<TranscriptionEntry>();
            }
            
            var json = await File.ReadAllTextAsync(transcriptionsFile);
            var transcriptions = JsonSerializer.Deserialize<List<TranscriptionEntry>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new List<TranscriptionEntry>();
            
            return transcriptions.OrderBy(t => t.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transcriptions for date: {Date}", date);
            return new List<TranscriptionEntry>();
        }
    }

    public async Task<List<TranscriptionEntry>> SearchTranscriptionsAsync(string searchTerm, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var results = new List<TranscriptionEntry>();
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;
            
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                var dailyTranscriptions = await GetTranscriptionsAsync(date);
                var matchingTranscriptions = dailyTranscriptions
                    .Where(t => t.FullText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                results.AddRange(matchingTranscriptions);
            }
            
            return results.OrderByDescending(t => t.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search transcriptions for term: {SearchTerm}", searchTerm);
            return new List<TranscriptionEntry>();
        }
    }

    public async Task<List<TranscriptionEntry>> GetTranscriptionHistoryAsync()
    {
        try
        {
            var allTranscriptions = new List<TranscriptionEntry>();
            
            if (Directory.Exists(_transcriptionsPath))
            {
                var directories = Directory.GetDirectories(_transcriptionsPath);
                
                foreach (var directory in directories)
                {
                    var folderName = Path.GetFileName(directory);
                    if (DateTime.TryParseExact(folderName, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var folderDate))
                    {
                        var dailyTranscriptions = await GetTranscriptionsAsync(folderDate);
                        allTranscriptions.AddRange(dailyTranscriptions);
                    }
                }
            }
            
            return allTranscriptions.OrderByDescending(t => t.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transcription history");
            return new List<TranscriptionEntry>();
        }
    }

    public async Task<int> CleanupOldTranscriptionsAsync(int retentionDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.Today.AddDays(-retentionDays);
            var deletedCount = 0;
            
            if (Directory.Exists(_transcriptionsPath))
            {
                var directories = Directory.GetDirectories(_transcriptionsPath);
                
                foreach (var directory in directories)
                {
                    var folderName = Path.GetFileName(directory);
                    if (DateTime.TryParseExact(folderName, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var folderDate))
                    {
                        if (folderDate < cutoffDate)
                        {
                            Directory.Delete(directory, true);
                            deletedCount++;
                            _logger.LogInformation("Deleted old transcription folder: {FolderName}", folderName);
                        }
                    }
                }
            }
            
            _logger.LogInformation("Cleanup completed: {DeletedCount} folders removed", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old transcriptions");
            throw;
        }
    }

    public async Task DeleteTranscriptionAsync(string id)
    {
        try
        {
            if (Directory.Exists(_transcriptionsPath))
            {
                var directories = Directory.GetDirectories(_transcriptionsPath);
                
                foreach (var directory in directories)
                {
                    var transcriptionsFile = Path.Combine(directory, "transcriptions.json");
                    if (File.Exists(transcriptionsFile))
                    {
                        lock (_lockObject)
                        {
                            var json = File.ReadAllText(transcriptionsFile);
                            var transcriptions = JsonSerializer.Deserialize<List<TranscriptionEntry>>(json, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            }) ?? new List<TranscriptionEntry>();
                            
                            var originalCount = transcriptions.Count;
                            transcriptions.RemoveAll(t => t.Id == id);
                            
                            if (transcriptions.Count < originalCount)
                            {
                                var updatedJson = JsonSerializer.Serialize(transcriptions, new JsonSerializerOptions
                                {
                                    WriteIndented = true,
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });
                                
                                File.WriteAllText(transcriptionsFile, updatedJson);
                                _logger.LogInformation("Deleted transcription entry: {Id}", id);
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete transcription entry: {Id}", id);
            throw;
        }
    }

    public async Task<int> GetTranscriptionCountAsync(DateTime? date = null)
    {
        try
        {
            var transcriptions = await GetTranscriptionsAsync(date);
            return transcriptions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transcription count for date: {Date}", date);
            return 0;
        }
    }
}
