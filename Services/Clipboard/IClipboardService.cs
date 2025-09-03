namespace CarelessWhisperV2.Services.Clipboard;

public interface IClipboardService
{
    Task<string> GetTextAsync();
    Task SetTextAsync(string text);
    Task<bool> ContainsTextAsync();
}

public class ClipboardException : Exception
{
    public ClipboardException(string message, Exception innerException) 
        : base(message, innerException) { }
}
