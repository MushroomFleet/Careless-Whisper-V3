using System.ComponentModel.DataAnnotations;

namespace CarelessWhisperV2.Models;

/// <summary>
/// Represents cached OpenRouter models data
/// </summary>
public class ModelCache
{
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string ApiKeyHash { get; set; } = "";
    public int ModelCount { get; set; }
    public List<OpenRouterModel> Models { get; set; } = new();
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// Information about the current cache state
/// </summary>
public class ModelCacheInfo
{
    public bool IsValid { get; set; }
    public bool Exists { get; set; }
    public DateTime? CachedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ModelCount { get; set; }
    public TimeSpan? Age { get; set; }
    public TimeSpan? TimeUntilExpiry { get; set; }
    
    /// <summary>
    /// Human-readable cache status
    /// </summary>
    public string StatusText
    {
        get
        {
            if (!Exists) return "No cached models";
            if (!IsValid) return "Cache expired";
            
            var ageText = Age?.TotalHours switch
            {
                < 1 => $"{Age?.TotalMinutes:F0} minutes old",
                < 24 => $"{Age?.TotalHours:F1} hours old",
                _ => $"{Age?.TotalDays:F1} days old"
            };
            
            return $"{ModelCount} models cached ({ageText})";
        }
    }
}

/// <summary>
/// Enhanced OpenRouter model with additional metadata for better caching and display
/// </summary>
public class OpenRouterModelExtended : OpenRouterModel
{
    public List<string> Modalities { get; set; } = new(); // text, image, etc.
    public string? Architecture { get; set; }
    public string? Provider { get; set; }
    public bool IsDeprecated { get; set; }
    public decimal? CompletionPrice { get; set; }
    public DateTime? LastUpdated { get; set; }
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}
