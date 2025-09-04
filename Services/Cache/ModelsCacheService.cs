using CarelessWhisperV2.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace CarelessWhisperV2.Services.Cache;

public class ModelsCacheService : IModelsCacheService
{
    private readonly ILogger<ModelsCacheService> _logger;
    private readonly string _cacheDirectory;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24); // 24 hour cache
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ModelsCacheService(ILogger<ModelsCacheService> logger)
    {
        _logger = logger;
        
        // Store cache in AppData/Roaming/CarelessWhisperV3/cache
        var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        _cacheDirectory = Path.Combine(appDataPath, "CarelessWhisperV3", "cache");
        
        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheDirectory);
        _logger.LogInformation($"Models cache directory: {_cacheDirectory}");
    }

    public async Task<List<OpenRouterModel>?> GetCachedModelsAsync(string apiKeyHash)
    {
        try
        {
            var cacheFile = GetCacheFilePath(apiKeyHash);
            
            if (!File.Exists(cacheFile))
            {
                _logger.LogDebug($"No cache file found for API key hash: {apiKeyHash}");
                return null;
            }

            var cacheContent = await File.ReadAllTextAsync(cacheFile);
            var cache = JsonSerializer.Deserialize<ModelCache>(cacheContent, JsonOptions);
            
            if (cache == null)
            {
                _logger.LogWarning($"Failed to deserialize cache file: {cacheFile}");
                return null;
            }

            // Check if cache is expired
            if (DateTime.UtcNow > cache.ExpiresAt)
            {
                _logger.LogDebug($"Cache expired for API key hash: {apiKeyHash}. Expired at: {cache.ExpiresAt}");
                return null;
            }

            // Validate cache integrity
            if (cache.Models?.Count != cache.ModelCount)
            {
                _logger.LogWarning($"Cache integrity check failed. Expected {cache.ModelCount} models, found {cache.Models?.Count ?? 0}");
                return null;
            }

            _logger.LogInformation($"Successfully loaded {cache.Models.Count} models from cache (cached at: {cache.CachedAt})");
            return cache.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading cached models for API key hash: {apiKeyHash}");
            return null;
        }
    }

    public async Task CacheModelsAsync(List<OpenRouterModel> models, string apiKeyHash)
    {
        try
        {
            var cacheFile = GetCacheFilePath(apiKeyHash);
            var now = DateTime.UtcNow;
            
            var cache = new ModelCache
            {
                CachedAt = now,
                ExpiresAt = now.Add(_defaultExpiration),
                ApiKeyHash = apiKeyHash,
                ModelCount = models.Count,
                Models = models,
                Version = "1.0"
            };

            var json = JsonSerializer.Serialize(cache, JsonOptions);
            
            // Write to temp file first, then move to prevent corruption
            var tempFile = cacheFile + ".tmp";
            await File.WriteAllTextAsync(tempFile, json);
            
            // Atomic move
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
            File.Move(tempFile, cacheFile);

            _logger.LogInformation($"Successfully cached {models.Count} models (expires: {cache.ExpiresAt})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error caching models for API key hash: {apiKeyHash}");
            throw;
        }
    }

    public async Task<bool> IsCacheValidAsync(string apiKeyHash)
    {
        try
        {
            var cacheFile = GetCacheFilePath(apiKeyHash);
            
            if (!File.Exists(cacheFile))
                return false;

            var cacheContent = await File.ReadAllTextAsync(cacheFile);
            var cache = JsonSerializer.Deserialize<ModelCache>(cacheContent, JsonOptions);
            
            return cache != null && DateTime.UtcNow <= cache.ExpiresAt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking cache validity for API key hash: {apiKeyHash}");
            return false;
        }
    }

    public async Task InvalidateCacheAsync(string apiKeyHash)
    {
        try
        {
            var cacheFile = GetCacheFilePath(apiKeyHash);
            
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
                _logger.LogInformation($"Cache invalidated for API key hash: {apiKeyHash}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error invalidating cache for API key hash: {apiKeyHash}");
            throw;
        }
    }

    public async Task ClearAllCacheAsync()
    {
        try
        {
            var cacheFiles = Directory.GetFiles(_cacheDirectory, "models_*.json");
            
            foreach (var file in cacheFiles)
            {
                File.Delete(file);
            }
            
            _logger.LogInformation($"Cleared {cacheFiles.Length} cache files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache");
            throw;
        }
    }

    public async Task<ModelCacheInfo?> GetCacheInfoAsync(string apiKeyHash)
    {
        try
        {
            var cacheFile = GetCacheFilePath(apiKeyHash);
            
            if (!File.Exists(cacheFile))
            {
                return new ModelCacheInfo
                {
                    Exists = false,
                    IsValid = false,
                    ModelCount = 0
                };
            }

            var cacheContent = await File.ReadAllTextAsync(cacheFile);
            var cache = JsonSerializer.Deserialize<ModelCache>(cacheContent, JsonOptions);
            
            if (cache == null)
            {
                return new ModelCacheInfo
                {
                    Exists = true,
                    IsValid = false,
                    ModelCount = 0
                };
            }

            var now = DateTime.UtcNow;
            var age = now - cache.CachedAt;
            var timeUntilExpiry = cache.ExpiresAt - now;
            
            return new ModelCacheInfo
            {
                Exists = true,
                IsValid = now <= cache.ExpiresAt,
                CachedAt = cache.CachedAt,
                ExpiresAt = cache.ExpiresAt,
                ModelCount = cache.ModelCount,
                Age = age,
                TimeUntilExpiry = timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting cache info for API key hash: {apiKeyHash}");
            return null;
        }
    }

    private string GetCacheFilePath(string apiKeyHash)
    {
        // Use first 8 characters of hash for filename to avoid long paths
        var shortHash = apiKeyHash.Length > 8 ? apiKeyHash.Substring(0, 8) : apiKeyHash;
        return Path.Combine(_cacheDirectory, $"models_{shortHash}.json");
    }

    /// <summary>
    /// Creates a secure hash of the API key for cache identification
    /// </summary>
    public static string HashApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "default";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Cleanup old cache files (called periodically)
    /// </summary>
    public async Task CleanupExpiredCacheAsync()
    {
        try
        {
            var cacheFiles = Directory.GetFiles(_cacheDirectory, "models_*.json");
            var deletedCount = 0;

            foreach (var file in cacheFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var cache = JsonSerializer.Deserialize<ModelCache>(content, JsonOptions);
                    
                    if (cache != null && DateTime.UtcNow > cache.ExpiresAt)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error processing cache file {file} during cleanup");
                    // Delete corrupted cache files
                    File.Delete(file);
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation($"Cleaned up {deletedCount} expired/corrupted cache files");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }
}
