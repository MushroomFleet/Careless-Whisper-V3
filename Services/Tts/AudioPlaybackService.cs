using NAudio.Wave;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CarelessWhisperV2.Services.Tts;

public class AudioPlaybackService : IAudioPlaybackService
{
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioReader;
    private readonly object _playbackLock = new object();
    private readonly ILogger<AudioPlaybackService> _logger;
    private bool _disposed;
    private float _volume = 1.0f;
    
    // DIAGNOSTIC: Audio playback tracking
    private int _playbackInvocationCount = 0;

    public bool IsPlaying
    {
        get
        {
            lock (_playbackLock)
            {
                return _waveOut?.PlaybackState == PlaybackState.Playing;
            }
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Max(0.0f, Math.Min(1.0f, value));
            lock (_playbackLock)
            {
                if (_waveOut != null)
                {
                    _waveOut.Volume = _volume;
                }
            }
        }
    }

    public AudioPlaybackService(ILogger<AudioPlaybackService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PlayAudioAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        _playbackInvocationCount++;
        _logger.LogInformation("DIAGNOSTIC TTS: PlayAudioAsync called #{Count}, data size: {Size} bytes", 
            _playbackInvocationCount, audioData?.Length ?? 0);
        
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioPlaybackService));

        if (audioData == null || audioData.Length == 0)
        {
            _logger.LogWarning("Cannot play empty audio data");
            return false;
        }

        try
        {
            // Create temporary file for NAudio
            var tempFile = Path.GetTempFileName() + ".wav";
            
            try
            {
                await File.WriteAllBytesAsync(tempFile, audioData, cancellationToken);
                return await PlayAudioFileAsync(tempFile, cancellationToken);
            }
            finally
            {
                // Clean up temporary file
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary audio file: {Path}", tempFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play audio from byte array");
            return false;
        }
    }

    public async Task<bool> PlayAudioFileAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioPlaybackService));

        if (!File.Exists(audioFilePath))
        {
            _logger.LogError("Audio file not found: {Path}", audioFilePath);
            return false;
        }

        try
        {
            lock (_playbackLock)
            {
                // Stop any existing playback
                StopPlayback();
                
                // Create new audio reader and wave output
                _audioReader = new AudioFileReader(audioFilePath);
                _waveOut = new WaveOutEvent();
                _waveOut.Volume = _volume;
                
                _waveOut.Init(_audioReader);
                _waveOut.Play();
                
                _logger.LogDebug("Started audio playback: {Path}", audioFilePath);
            }

            // Wait for playback completion or cancellation
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                PlaybackState currentState;
                lock (_playbackLock)
                {
                    currentState = _waveOut?.PlaybackState ?? PlaybackState.Stopped;
                }
                
                if (currentState == PlaybackState.Stopped)
                {
                    _logger.LogDebug("Audio playback completed: {Path}", audioFilePath);
                    break;
                }
                
                await Task.Delay(50, cancellationToken);
            }

            // Clean up after playback completion
            lock (_playbackLock)
            {
                StopPlayback();
            }
            
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Audio playback cancelled: {Path}", audioFilePath);
            lock (_playbackLock)
            {
                StopPlayback();
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play audio file: {Path}", audioFilePath);
            lock (_playbackLock)
            {
                StopPlayback();
            }
            return false;
        }
    }

    public void StopPlayback()
    {
        lock (_playbackLock)
        {
            try
            {
                if (_waveOut != null)
                {
                    if (_waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        _waveOut.Stop();
                        _logger.LogDebug("Audio playback stopped");
                    }
                    
                    _waveOut.Dispose();
                    _waveOut = null;
                }
                
                if (_audioReader != null)
                {
                    _audioReader.Dispose();
                    _audioReader = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping audio playback");
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_playbackLock)
            {
                StopPlayback();
                _disposed = true;
            }
            
            _logger.LogDebug("AudioPlaybackService disposed");
        }
    }
}
