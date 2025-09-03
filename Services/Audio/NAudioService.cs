using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Models;
using System.IO;

namespace CarelessWhisperV2.Services.Audio;

public class NAudioService : IAudioService, IDisposable
{
    private WasapiCapture? _capture;
    private WaveFileWriter? _writer;
    private readonly object _lockObject = new object();
    private readonly ILogger<NAudioService> _logger;
    private bool _disposed = false;
    private string _currentFilePath = "";
    private WaveFormat? _sourceFormat;

    // Whisper-optimal format: 16kHz, 16-bit, mono
    public static readonly WaveFormat WhisperFormat = new WaveFormat(16000, 16, 1);

    static NAudioService()
    {
        // Initialize MediaFoundation for resampling
        MediaFoundationApi.Startup();
    }

    public event EventHandler<AudioRecordingEventArgs>? RecordingStarted;
    public event EventHandler<AudioRecordingEventArgs>? RecordingStopped;

    public NAudioService(ILogger<NAudioService> logger)
    {
        _logger = logger;
    }

    public Task StartRecordingAsync(string outputPath)
    {
        return Task.Run(() =>
        {
            lock (_lockObject)
            {
                if (_capture != null || _disposed)
                {
                    _logger.LogWarning("Recording already in progress or service disposed");
                    return;
                }

                try
                {
                    _currentFilePath = outputPath;

                    // Use WASAPI in shared mode to get device's native format
                    _capture = new WasapiCapture();
                    _sourceFormat = _capture.WaveFormat;

                    _logger.LogInformation("Recording format - Source: {SourceFormat}, Target: {TargetFormat}", 
                        _sourceFormat, WhisperFormat);

                    // Record in source format first, then convert if needed
                    _writer = new WaveFileWriter(outputPath, _sourceFormat);

                    _capture.DataAvailable += OnDataAvailable;
                    _capture.RecordingStopped += OnRecordingStopped;

                    _capture.StartRecording();
                    
                    _logger.LogInformation("Recording started: {FilePath}, Source: {SourceFormat}", 
                        outputPath, _sourceFormat);
                    RecordingStarted?.Invoke(this, new AudioRecordingEventArgs { FilePath = outputPath });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start recording");
                    CleanupRecording();
                    throw;
                }
            }
        });
    }

    public Task StopRecordingAsync()
    {
        return Task.Run(() =>
        {
            lock (_lockObject)
            {
                if (_capture != null)
                {
                    _logger.LogInformation("Stopping recording: {FilePath}", _currentFilePath);
                    _capture.StopRecording();
                }
            }
        });
    }

    public bool IsRecording
    {
        get
        {
            lock (_lockObject)
            {
                return _capture != null && _capture.CaptureState == CaptureState.Capturing;
            }
        }
    }

    public List<AudioDevice> GetAvailableMicrophones()
    {
        var devices = new List<AudioDevice>();
        
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            
            foreach (var endpoint in endpoints)
            {
                devices.Add(new AudioDevice
                {
                    Id = endpoint.ID,
                    Name = endpoint.FriendlyName,
                    IsDefault = endpoint.ID == defaultDevice.ID
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate audio devices");
        }

        return devices;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_lockObject)
        {
            if (_writer != null && !_disposed)
            {
                try
                {
                    // Write audio data in source format
                    _writer.Write(e.Buffer, 0, e.BytesRecorded);
                    _writer.Flush(); // Ensure immediate write for real-time processing
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error writing audio data");
                }
            }
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        lock (_lockObject)
        {
            var filePath = _currentFilePath;
            var sourceFormat = _sourceFormat;
            CleanupRecording();
            
            if (e.Exception != null)
            {
                _logger.LogError(e.Exception, "Recording stopped due to error: {FilePath}", filePath);
                RecordingStopped?.Invoke(this, new AudioRecordingEventArgs 
                { 
                    FilePath = filePath, 
                    Exception = e.Exception 
                });
            }
            else
            {
                _logger.LogInformation("Recording completed successfully: {FilePath}", filePath);
                
                // Convert to Whisper format if needed
                if (sourceFormat != null && !sourceFormat.Equals(WhisperFormat))
                {
                    Task.Run(() => ConvertToWhisperFormat(filePath, sourceFormat));
                }
                else
                {
                    RecordingStopped?.Invoke(this, new AudioRecordingEventArgs { FilePath = filePath });
                }
            }
        }
    }

    private void CleanupRecording()
    {
        _writer?.Dispose();
        _writer = null;
        _capture?.Dispose();
        _capture = null;
        _currentFilePath = "";
    }

    private async Task ConvertToWhisperFormat(string filePath, WaveFormat sourceFormat)
    {
        try
        {
            _logger.LogInformation("Converting audio from {SourceFormat} to {TargetFormat}: {FilePath}", 
                sourceFormat, WhisperFormat, filePath);

            var tempPath = filePath + ".temp";

            using (var reader = new WaveFileReader(filePath))
            {
                using var resampler = new MediaFoundationResampler(reader, WhisperFormat)
                {
                    ResamplerQuality = 60 // High quality resampling
                };

                WaveFileWriter.CreateWaveFile(tempPath, resampler);
            }

            // Replace original file with converted version
            if (File.Exists(tempPath))
            {
                File.Delete(filePath);
                File.Move(tempPath, filePath);
                
                _logger.LogInformation("Audio conversion completed successfully: {FilePath}", filePath);
                RecordingStopped?.Invoke(this, new AudioRecordingEventArgs { FilePath = filePath });
            }
            else
            {
                throw new InvalidOperationException("Conversion failed - temporary file not created");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio conversion failed: {FilePath}", filePath);
            
            // Still notify completion even if conversion failed
            RecordingStopped?.Invoke(this, new AudioRecordingEventArgs 
            { 
                FilePath = filePath, 
                Exception = new Exception($"Audio format conversion failed: {ex.Message}", ex)
            });
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lockObject)
        {
            CleanupRecording();
            _disposed = true;
        }
        
        _logger.LogInformation("NAudioService disposed");
    }
}
