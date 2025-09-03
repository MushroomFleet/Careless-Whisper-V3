using Whisper.net;
using Whisper.net.Ggml;
using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Models;
using System.IO;

namespace CarelessWhisperV2.Services.Transcription;

public class WhisperTranscriptionService : ITranscriptionService
{
    private readonly ILogger<WhisperTranscriptionService> _logger;
    private WhisperFactory? _whisperFactory;
    private string _modelPath = "";
    private bool _disposed = false;

    public bool IsInitialized => _whisperFactory != null;
    public event EventHandler<TranscriptionProgressEventArgs>? ProgressChanged;

    public WhisperTranscriptionService(ILogger<WhisperTranscriptionService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(string modelSize = "Base")
    {
        try
        {
            ProgressChanged?.Invoke(this, new TranscriptionProgressEventArgs 
            { 
                ProgressPercentage = 0, 
                Status = "Initializing Whisper model..." 
            });

            var modelType = ParseModelSize(modelSize);
            _modelPath = $"ggml-{modelType.ToString().ToLower()}.bin";
            
            if (!File.Exists(_modelPath))
            {
                _logger.LogInformation("Downloading Whisper model: {ModelType}", modelType);
                ProgressChanged?.Invoke(this, new TranscriptionProgressEventArgs 
                { 
                    ProgressPercentage = 25, 
                    Status = $"Downloading {modelType} model..." 
                });
                
                await DownloadModelAsync(modelType);
            }
            
            ProgressChanged?.Invoke(this, new TranscriptionProgressEventArgs 
            { 
                ProgressPercentage = 75, 
                Status = "Loading model..." 
            });
            
            _whisperFactory = WhisperFactory.FromPath(_modelPath);
            
            ProgressChanged?.Invoke(this, new TranscriptionProgressEventArgs 
            { 
                ProgressPercentage = 100, 
                Status = "Model ready" 
            });
            
            _logger.LogInformation("Whisper model initialized: {ModelType}", modelType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Whisper model");
            throw;
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (_whisperFactory == null)
        {
            throw new InvalidOperationException("Transcription service not initialized. Call InitializeAsync first.");
        }

        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        try
        {
            _logger.LogInformation("Starting transcription: {FilePath}", audioFilePath);
            
            ProgressChanged?.Invoke(this, new TranscriptionProgressEventArgs 
            { 
                ProgressPercentage = 0, 
                Status = "Processing audio..." 
            });

            using var processor = _whisperFactory.CreateBuilder()
                .WithLanguage("auto")
                .WithThreads(System.Environment.ProcessorCount)
                .Build();
            
            using var fileStream = File.OpenRead(audioFilePath);
            var segments = new List<TranscriptionSegment>();
            var totalProgress = 0;

            await foreach (var result in processor.ProcessAsync(fileStream, cancellationToken))
            {
                segments.Add(new TranscriptionSegment
                {
                    Start = result.Start,
                    End = result.End,
                    Text = result.Text.Trim()
                });

                // Update progress (rough estimation)
                totalProgress = Math.Min(90, totalProgress + 10);
                ProgressChanged?.Invoke(this, new TranscriptionProgressEventArgs 
                { 
                    ProgressPercentage = totalProgress, 
                    Status = "Transcribing..." 
                });
            }

            ProgressChanged?.Invoke(this, new TranscriptionProgressEventArgs 
            { 
                ProgressPercentage = 100, 
                Status = "Transcription complete" 
            });

            var transcriptionResult = new TranscriptionResult
            {
                Segments = segments,
                FullText = string.Join(" ", segments.Select(s => s.Text)),
                Language = "auto" // Could detect language from processor
            };

            _logger.LogInformation("Transcription completed: {CharCount} characters", transcriptionResult.FullText.Length);
            return transcriptionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed: {FilePath}", audioFilePath);
            throw;
        }
    }

    private static GgmlType ParseModelSize(string modelSize)
    {
        return modelSize.ToLower() switch
        {
            "tiny" => GgmlType.Tiny,
            "base" => GgmlType.Base,
            "small" => GgmlType.Small,
            "medium" => GgmlType.Medium,
            _ => GgmlType.Base // Default fallback
        };
    }

    private async Task DownloadModelAsync(GgmlType modelType)
    {
        try
        {
            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType);
            using var fileWriter = File.OpenWrite(_modelPath);
            await modelStream.CopyToAsync(fileWriter);
            
            _logger.LogInformation("Model downloaded successfully: {ModelPath}", _modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download model: {ModelType}", modelType);
            
            // Clean up partial download
            if (File.Exists(_modelPath))
            {
                try
                {
                    File.Delete(_modelPath);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to delete partial model file");
                }
            }
            
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _whisperFactory?.Dispose();
            _disposed = true;
            _logger.LogInformation("WhisperTranscriptionService disposed");
        }
    }
}
