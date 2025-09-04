using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CarelessWhisperV2.Services.Settings;

public class JsonSettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<JsonSettingsService> _logger;

    public JsonSettingsService(ILogger<JsonSettingsService> logger)
    {
        _logger = logger;
        
        var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "CarelessWhisperV2");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<T> LoadSettingsAsync<T>() where T : new()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation("Settings file not found, creating default settings");
                var defaultSettings = new T();
                await SaveSettingsAsync(defaultSettings);
                return defaultSettings;
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            return settings ?? new T();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            return new T();
        }
    }

    public async Task SaveSettingsAsync<T>(T settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_settingsPath, json);
            
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(typeof(T), settings));
            _logger.LogDebug("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}
