using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Services.Logging;

namespace CarelessWhisperV2;

public class TestTranscriptionLogger
{
    public static async Task Main(string[] args)
    {
        // Create a simple logger
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<FileTranscriptionLogger>();
        
        // Create the transcription logger
        var transcriptionLogger = new FileTranscriptionLogger(logger);
        
        try
        {
            Console.WriteLine("Testing FileTranscriptionLogger...");
            
            // Test getting transcription history
            var history = await transcriptionLogger.GetTranscriptionHistoryAsync();
            
            Console.WriteLine($"Found {history.Count} transcriptions in history:");
            
            foreach (var entry in history.Take(5)) // Show first 5 entries
            {
                Console.WriteLine($"- {entry.Timestamp:yyyy-MM-dd HH:mm:ss}: {entry.FullText.Substring(0, Math.Min(50, entry.FullText.Length))}...");
            }
            
            if (history.Count > 5)
            {
                Console.WriteLine($"... and {history.Count - 5} more entries");
            }
            
            Console.WriteLine("\nTest completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
