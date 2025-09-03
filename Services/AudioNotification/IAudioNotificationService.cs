namespace CarelessWhisperV2.Services.AudioNotification;

public interface IAudioNotificationService
{
    Task PlayNotificationAsync(NotificationType type);
    Task<bool> TestAudioFileAsync(string filePath);
    bool IsAudioFileValid(string filePath);
    void SetVolume(double volume);
}

public enum NotificationType
{
    SpeechToText,
    LlmResponse
}
