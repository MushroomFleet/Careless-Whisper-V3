using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Settings;

public interface ISettingsService
{
    Task<T> LoadSettingsAsync<T>() where T : new();
    Task SaveSettingsAsync<T>(T settings);
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}

public class SettingsChangedEventArgs : EventArgs
{
    public Type SettingsType { get; }
    public object Settings { get; }

    public SettingsChangedEventArgs(Type settingsType, object settings)
    {
        SettingsType = settingsType;
        Settings = settings;
    }
}
