using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CarelessWhisperV2.Services.Environment;

public class EnvironmentService : IEnvironmentService
{
    private readonly ILogger<EnvironmentService> _logger;
    private readonly string _envFilePath;
    private readonly string _appDataPath;

    public EnvironmentService(ILogger<EnvironmentService> logger)
    {
        _logger = logger;
        _appDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "CarelessWhisperV3");
        _envFilePath = Path.Combine(_appDataPath, ".env");
        
        // Ensure directory exists
        Directory.CreateDirectory(_appDataPath);
    }

    public async Task<string> GetApiKeyAsync()
    {
        try
        {
            if (!File.Exists(_envFilePath))
                return "";

            var lines = await File.ReadAllLinesAsync(_envFilePath);
            var apiKeyLine = lines.FirstOrDefault(line => line.StartsWith("OPENROUTER_API_KEY="));
            
            if (apiKeyLine != null)
            {
                var encryptedKey = apiKeyLine.Substring("OPENROUTER_API_KEY=".Length);
                return DecryptString(encryptedKey);
            }

            return "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read API key from .env file");
            return "";
        }
    }

    public async Task SaveApiKeyAsync(string apiKey)
    {
        try
        {
            var encryptedKey = EncryptString(apiKey);
            var content = $"OPENROUTER_API_KEY={encryptedKey}";
            await File.WriteAllTextAsync(_envFilePath, content);
            _logger.LogInformation("API key saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save API key to .env file");
            throw;
        }
    }

    public async Task<bool> ApiKeyExistsAsync()
    {
        var apiKey = await GetApiKeyAsync();
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    public async Task DeleteApiKeyAsync()
    {
        try
        {
            if (File.Exists(_envFilePath))
            {
                File.Delete(_envFilePath);
                _logger.LogInformation("API key deleted successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete API key file");
            throw;
        }
    }

    private string EncryptString(string plainText)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            var encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt string");
            throw;
        }
    }

    private string DecryptString(string encryptedText)
    {
        try
        {
            var data = Convert.FromBase64String(encryptedText);
            var decryptedData = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt string");
            return "";
        }
    }
}
