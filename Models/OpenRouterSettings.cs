using System.ComponentModel.DataAnnotations;

namespace CarelessWhisperV2.Models;

public class OpenRouterSettings : IValidatableObject
{
    public string ApiKey { get; set; } = "";
    public string SelectedModel { get; set; } = "anthropic/claude-sonnet-4";
    public string SystemPrompt { get; set; } = "You are a helpful assistant. Please provide a clear, concise response to the user's voice input.";
    public bool EnableStreaming { get; set; } = true;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public List<OpenRouterModel> AvailableModels { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            yield return new ValidationResult("OpenRouter API key is required");

        if (string.IsNullOrWhiteSpace(SelectedModel))
            yield return new ValidationResult("Model selection is required");

        if (Temperature < 0 || Temperature > 2)
            yield return new ValidationResult("Temperature must be between 0 and 2");

        if (MaxTokens < 1 || MaxTokens > 4000)
            yield return new ValidationResult("Max tokens must be between 1 and 4000");
    }
}

public class OpenRouterModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal PricePerMToken { get; set; }
    public int ContextLength { get; set; }
    public bool SupportsStreaming { get; set; } = true;
}
