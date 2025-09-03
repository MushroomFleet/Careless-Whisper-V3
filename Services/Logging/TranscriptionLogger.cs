using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Logging;

public class TranscriptionLogger : ITranscriptionLogger
{
    private readonly ILogger<TranscriptionLogger> _logger;
    private readonly ApplicationSettings _settings;
    private readonly string _logDirectory;
    private readonly string _logFilePath;

    public TranscriptionLogger(ILogger<TranscriptionLogger> logger, IOptions<ApplicationSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        
        var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        _logDirectory = Path.Combine(appDataPath, "CarelessWhisperV2", "transcriptions");
        
        // We'll use date-based folders, but keep this path for compatibility
        _logFilePath = Path.Combine(_logDirectory, "transcriptions.jsonl");
        
        Directory.CreateDirectory(_logDirectory);
    }

    public async Task LogTranscriptionAsync(TranscriptionEntry entry)
    {
        if (!_settings.Logging.EnableTranscriptionLogging)
            return;

        try
        {
            entry.Timestamp = DateTime.Now;
            if (string.IsNullOrEmpty(entry.Id))
                entry.Id = Guid.NewGuid().ToString();

            var json = JsonSerializer.Serialize(entry);
            await File.AppendAllTextAsync(_logFilePath, json + System.Environment.NewLine);
            
            _logger.LogInformation("Transcription logged: {Id}", entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log transcription entry");
            throw;
        }
    }

    public async Task<List<TranscriptionEntry>> GetTranscriptionsAsync(DateTime? date = null)
    {
        var entries = new List<TranscriptionEntry>();
        
        if (!Directory.Exists(_logDirectory))
            return entries;

        try
        {
            // Check for old single file format first for backward compatibility
            if (File.Exists(_logFilePath))
            {
                var legacyEntries = await ReadLegacyFormatAsync(date);
                entries.AddRange(legacyEntries);
            }

            // Read from date-based folder structure
            var dateFolders = Directory.GetDirectories(_logDirectory)
                .Where(d => DateTime.TryParseExact(Path.GetFileName(d), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                .ToArray();

            foreach (var dateFolder in dateFolders)
            {
                var folderDate = DateTime.ParseExact(Path.GetFileName(dateFolder), "yyyy-MM-dd", null);
                
                // Skip if we're filtering by date and this folder doesn't match
                if (date.HasValue && folderDate.Date != date.Value.Date)
                    continue;

                var jsonFiles = Directory.GetFiles(dateFolder, "*.json");
                
                foreach (var jsonFile in jsonFiles)
                {
                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(jsonFile);
                        var entry = JsonSerializer.Deserialize<TranscriptionEntry>(jsonContent);
                        
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize transcription entry from file: {File}", jsonFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read transcription file: {File}", jsonFile);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read transcription files from directory: {Directory}", _logDirectory);
            throw;
        }

        return entries.OrderByDescending(e => e.Timestamp).ToList();
    }

    private async Task<List<TranscriptionEntry>> ReadLegacyFormatAsync(DateTime? date = null)
    {
        var entries = new List<TranscriptionEntry>();
        
        try
        {
            var lines = await File.ReadAllLinesAsync(_logFilePath);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                try
                {
                    var entry = JsonSerializer.Deserialize<TranscriptionEntry>(line);
                    if (entry != null)
                    {
                        if (date == null || entry.Timestamp.Date == date.Value.Date)
                        {
                            entries.Add(entry);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize transcription entry from line: {Line}", line);
                }
            }
            
            _logger.LogInformation("Read {Count} entries from legacy format", entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read legacy transcription log file");
            throw;
        }

        return entries;
    }

    public async Task<List<TranscriptionEntry>> GetTranscriptionHistoryAsync()
    {
        return await GetTranscriptionsAsync();
    }

    public async Task<List<TranscriptionEntry>> SearchTranscriptionsAsync(string searchTerm, DateTime? startDate = null, DateTime? endDate = null)
    {
        var allEntries = await GetTranscriptionsAsync();
        
        var filteredEntries = allEntries.Where(entry =>
        {
            // Date filter
            if (startDate.HasValue && entry.Timestamp.Date < startDate.Value.Date)
                return false;
            if (endDate.HasValue && entry.Timestamp.Date > endDate.Value.Date)
                return false;
                
            // Text search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                return entry.FullText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            }
            
            return true;
        }).ToList();

        return filteredEntries;
    }

    public async Task<int> CleanupOldTranscriptionsAsync(int retentionDays = 30)
    {
        if (!Directory.Exists(_logDirectory))
            return 0;

        try
        {
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var deletedCount = 0;

            // Clean up legacy format file
            if (File.Exists(_logFilePath))
            {
                var allEntries = await ReadLegacyFormatAsync();
                var entriesToKeep = allEntries.Where(entry => entry.Timestamp >= cutoffDate).ToList();
                var legacyDeletedCount = allEntries.Count - entriesToKeep.Count;
                
                if (legacyDeletedCount > 0)
                {
                    var tempFilePath = _logFilePath + ".tmp";
                    
                    await using (var writer = new StreamWriter(tempFilePath))
                    {
                        foreach (var entry in entriesToKeep)
                        {
                            var json = JsonSerializer.Serialize(entry);
                            await writer.WriteLineAsync(json);
                        }
                    }
                    
                    File.Move(tempFilePath, _logFilePath, true);
                    deletedCount += legacyDeletedCount;
                }
            }

            // Clean up date-based folders
            var dateFolders = Directory.GetDirectories(_logDirectory)
                .Where(d => DateTime.TryParseExact(Path.GetFileName(d), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                .ToArray();

            foreach (var dateFolder in dateFolders)
            {
                var folderDate = DateTime.ParseExact(Path.GetFileName(dateFolder), "yyyy-MM-dd", null);
                
                // If entire folder is older than cutoff, delete the whole folder
                if (folderDate.Date < cutoffDate.Date)
                {
                    var jsonFiles = Directory.GetFiles(dateFolder, "*.json");
                    deletedCount += jsonFiles.Length;
                    
                    // Clean up associated audio files first
                    foreach (var jsonFile in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(jsonFile);
                            var entry = JsonSerializer.Deserialize<TranscriptionEntry>(jsonContent);
                            
                            if (entry != null && !string.IsNullOrEmpty(entry.AudioFilePath) && File.Exists(entry.AudioFilePath))
                            {
                                try
                                {
                                    File.Delete(entry.AudioFilePath);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to delete audio file: {FilePath}", entry.AudioFilePath);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process file for cleanup: {File}", jsonFile);
                        }
                    }
                    
                    Directory.Delete(dateFolder, true);
                    _logger.LogDebug("Deleted date folder: {Folder}", dateFolder);
                }
            }
            
            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old transcription entries", deletedCount);
            }
            
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
        if (!Directory.Exists(_logDirectory))
            return;

        try
        {
            var allEntries = await GetTranscriptionsAsync();
            var entryToDelete = allEntries.FirstOrDefault(e => e.Id == id);
            
            if (entryToDelete == null)
                return;

            var entryDeleted = false;

            // Check legacy format first
            if (File.Exists(_logFilePath))
            {
                var legacyEntries = await ReadLegacyFormatAsync();
                var legacyEntryToDelete = legacyEntries.FirstOrDefault(e => e.Id == id);
                
                if (legacyEntryToDelete != null)
                {
                    var entriesToKeep = legacyEntries.Where(entry => entry.Id != id).ToList();
                    
                    var tempFilePath = _logFilePath + ".tmp";
                    
                    await using (var writer = new StreamWriter(tempFilePath))
                    {
                        foreach (var entry in entriesToKeep)
                        {
                            var json = JsonSerializer.Serialize(entry);
                            await writer.WriteLineAsync(json);
                        }
                    }
                    
                    File.Move(tempFilePath, _logFilePath, true);
                    entryDeleted = true;
                }
            }

            // Search through date-based folders
            if (!entryDeleted)
            {
                var dateFolders = Directory.GetDirectories(_logDirectory)
                    .Where(d => DateTime.TryParseExact(Path.GetFileName(d), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                    .ToArray();

                foreach (var dateFolder in dateFolders)
                {
                    var jsonFiles = Directory.GetFiles(dateFolder, "*.json");
                    
                    foreach (var jsonFile in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(jsonFile);
                            var entry = JsonSerializer.Deserialize<TranscriptionEntry>(jsonContent);
                            
                            if (entry != null && entry.Id == id)
                            {
                                // Found the entry to delete
                                File.Delete(jsonFile);
                                entryDeleted = true;
                                
                                _logger.LogDebug("Deleted transcription file: {File}", jsonFile);
                                
                                // Check if this was the only file in the folder
                                var remainingFiles = Directory.GetFiles(dateFolder, "*.json");
                                if (remainingFiles.Length == 0)
                                {
                                    try
                                    {
                                        Directory.Delete(dateFolder);
                                        _logger.LogDebug("Deleted empty date folder: {Folder}", dateFolder);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to delete empty folder: {Folder}", dateFolder);
                                    }
                                }
                                
                                break;
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize transcription entry from file: {File}", jsonFile);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to read transcription file: {File}", jsonFile);
                        }
                    }
                    
                    if (entryDeleted)
                        break;
                }
            }
            
            // Clean up associated audio file if it exists
            if (entryDeleted && !string.IsNullOrEmpty(entryToDelete.AudioFilePath) && File.Exists(entryToDelete.AudioFilePath))
            {
                try
                {
                    File.Delete(entryToDelete.AudioFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete audio file: {FilePath}", entryToDelete.AudioFilePath);
                }
            }
            
            if (entryDeleted)
            {
                _logger.LogInformation("Deleted transcription entry: {Id}", id);
            }
            else
            {
                _logger.LogWarning("Transcription entry not found for deletion: {Id}", id);
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
        var entries = await GetTranscriptionsAsync(date);
        return entries.Count;
    }
}
