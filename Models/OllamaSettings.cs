using System.ComponentModel.DataAnnotations;

namespace CarelessWhisperV2.Models;

public class OllamaSettings : IValidatableObject
{
    public string ServerUrl { get; set; } = "http://localhost:11434";
    public string SelectedModel { get; set; } = "";
    public string SystemPrompt { get; set; } = "You are a helpful assistant. Please provide a clear, concise response to the user's voice input.";
    public bool EnableStreaming { get; set; } = true;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public List<OllamaModel> AvailableModels { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
            yield return new ValidationResult("Ollama server URL is required");

        if (!Uri.TryCreate(ServerUrl, UriKind.Absolute, out var uri))
            yield return new ValidationResult("Ollama server URL must be a valid URL");

        if (string.IsNullOrWhiteSpace(SelectedModel))
            yield return new ValidationResult("Model selection is required");

        if (Temperature < 0 || Temperature > 2)
            yield return new ValidationResult("Temperature must be between 0 and 2");

        if (MaxTokens < 1 || MaxTokens > 4000)
            yield return new ValidationResult("Max tokens must be between 1 and 4000");
    }
}

public class OllamaModel
{
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
    public long Size { get; set; }
    public string Digest { get; set; } = "";
    public DateTime ModifiedAt { get; set; }
    public OllamaModelDetails? Details { get; set; }
}

public class OllamaModelDetails
{
    public string Format { get; set; } = "";
    public string Family { get; set; } = "";
    public List<string> Families { get; set; } = new();
    public long ParameterSize { get; set; }
    public long QuantizationLevel { get; set; }
}
