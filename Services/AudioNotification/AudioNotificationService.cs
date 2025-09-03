using CarelessWhisperV2.Models;
using CarelessWhisperV2.Services.Settings;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.IO;

namespace CarelessWhisperV2.Services.AudioNotification;

public class AudioNotificationService : IAudioNotificationService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AudioNotificationService> _logger;
    private ApplicationSettings _settings = new();
    private bool _disposed;

    public AudioNotificationService(ISettingsService settingsService, ILogger<AudioNotificationService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        
        // Subscribe to settings changes to keep settings up-to-date
        _settingsService.SettingsChanged += OnSettingsChanged;
        
        // Load settings
        _ = Task.Run(LoadSettingsAsync);
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            _settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            _logger.LogDebug("Audio notification settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load audio notification settings, using defaults");
            _settings = new ApplicationSettings();
        }
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.SettingsType == typeof(ApplicationSettings) && e.Settings is ApplicationSettings newSettings)
        {
            _settings = newSettings;
            _logger.LogDebug("Audio notification settings updated from settings change event");
        }
    }

    public async Task PlayNotificationAsync(NotificationType type)
    {
        if (!_settings.AudioNotification.EnableNotifications)
        {
            _logger.LogDebug("Audio notifications are disabled");
            return;
        }

        // Check if this notification type is enabled
        bool shouldPlay = type switch
        {
            NotificationType.SpeechToText => _settings.AudioNotification.PlayOnSpeechToText,
            NotificationType.LlmResponse => _settings.AudioNotification.PlayOnLlmResponse,
            _ => false
        };

        if (!shouldPlay)
        {
            _logger.LogDebug("Notification type {Type} is disabled", type);
            return;
        }

        var audioFilePath = _settings.AudioNotification.AudioFilePath;
        if (string.IsNullOrWhiteSpace(audioFilePath))
        {
            _logger.LogWarning("No audio file path configured for notifications");
            return;
        }

        try
        {
            await PlayAudioFileAsync(audioFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play notification audio: {FilePath}", audioFilePath);
            
            // If audio file fails, try to disable notifications to prevent repeated errors
            if (!File.Exists(audioFilePath))
            {
                _logger.LogWarning("Audio file not found, disabling notifications: {FilePath}", audioFilePath);
                _settings.AudioNotification.EnableNotifications = false;
                await _settingsService.SaveSettingsAsync(_settings);
            }
        }
    }

    public async Task<bool> TestAudioFileAsync(string filePath)
    {
        if (!IsAudioFileValid(filePath))
            return false;

        try
        {
            await PlayAudioFileAsync(filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test audio file: {FilePath}", filePath);
            return false;
        }
    }

    public bool IsAudioFileValid(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (!File.Exists(filePath))
            return false;

        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension == ".wav" || extension == ".mp3";
    }

    public void SetVolume(double volume)
    {
        // Volume will be applied during playback
        _settings.AudioNotification.Volume = Math.Clamp(volume, 0.0, 1.0);
    }

    private async Task PlayAudioFileAsync(string filePath)
    {
        await Task.Run(() =>
        {
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            
            switch (extension)
            {
                case ".wav":
                    PlayWavFile(filePath);
                    break;
                case ".mp3":
                    PlayMp3File(filePath);
                    break;
                default:
                    throw new NotSupportedException($"Audio format not supported: {extension}");
            }
        });
    }

    private void PlayWavFile(string filePath)
    {
        using var audioFile = new AudioFileReader(filePath);
        using var outputDevice = new WaveOutEvent();
        
        outputDevice.Init(audioFile);
        outputDevice.Volume = (float)_settings.AudioNotification.Volume;
        outputDevice.Play();
        
        // Wait for playback to complete
        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(100);
        }
    }

    private void PlayMp3File(string filePath)
    {
        using var mp3Reader = new Mp3FileReader(filePath);
        using var outputDevice = new WaveOutEvent();
        
        outputDevice.Init(mp3Reader);
        outputDevice.Volume = (float)_settings.AudioNotification.Volume;
        outputDevice.Play();
        
        // Wait for playback to complete
        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(100);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Unsubscribe from settings changes
            _settingsService.SettingsChanged -= OnSettingsChanged;
            
            _disposed = true;
            _logger.LogInformation("AudioNotificationService disposed");
        }
    }
}
