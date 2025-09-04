using CarelessWhisperV2.Models;

namespace CarelessWhisperV2.Services.Cache;

public interface IModelsCacheService
{
    /// <summary>
    /// Gets cached models if they exist and are not expired
    /// </summary>
    Task<List<OpenRouterModel>?> GetCachedModelsAsync(string apiKeyHash);
    
    /// <summary>
    /// Caches the models with expiration time
    /// </summary>
    Task CacheModelsAsync(List<OpenRouterModel> models, string apiKeyHash);
    
    /// <summary>
    /// Checks if cached models exist and are still valid
    /// </summary>
    Task<bool> IsCacheValidAsync(string apiKeyHash);
    
    /// <summary>
    /// Invalidates the cache for a specific API key
    /// </summary>
    Task InvalidateCacheAsync(string apiKeyHash);
    
    /// <summary>
    /// Clears all cached models
    /// </summary>
    Task ClearAllCacheAsync();
    
    /// <summary>
    /// Gets cache information for display
    /// </summary>
    Task<ModelCacheInfo?> GetCacheInfoAsync(string apiKeyHash);
}
