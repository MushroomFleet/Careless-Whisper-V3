namespace CarelessWhisperV2.Services.Environment;

public interface IEnvironmentService
{
    Task<string> GetApiKeyAsync();
    Task SaveApiKeyAsync(string apiKey);
    Task<bool> ApiKeyExistsAsync();
    Task DeleteApiKeyAsync();
}
