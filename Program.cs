using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Models;
using CarelessWhisperV2.Services.Settings;
using CarelessWhisperV2.Services.Clipboard;
using CarelessWhisperV2.Services.Audio;
using CarelessWhisperV2.Services.Transcription;
using CarelessWhisperV2.Services.Hotkeys;
using CarelessWhisperV2.Services.Logging;
using CarelessWhisperV2.Services.Orchestration;
using CarelessWhisperV2.Services.Environment;
using CarelessWhisperV2.Services.OpenRouter;
using CarelessWhisperV2.Services.Ollama;
using CarelessWhisperV2.Services.AudioNotification;
using CarelessWhisperV2.Services.Cache;

namespace CarelessWhisperV2;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Allocate console for WPF app to see output
        if (!AllocConsole())
        {
            AttachConsole(-1); // Attach to parent console if available
        }
        
        Console.WriteLine("DEPENDENCY INJECTION TEST: Starting...");
        
        try
        {
            Console.WriteLine("DI TEST: Creating host builder...");
            var builder = Host.CreateApplicationBuilder(args);
            
            Console.WriteLine("DI TEST: Configuring logging...");
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            
            Console.WriteLine("STARTUP: Registering all services for Careless Whisper V3...");
            
            // Configuration
            builder.Services.Configure<ApplicationSettings>(
                builder.Configuration.GetSection("ApplicationSettings"));
            
            // Core services
            builder.Services.AddSingleton<ISettingsService, JsonSettingsService>();
            builder.Services.AddSingleton<IClipboardService, ClipboardService>();
            builder.Services.AddSingleton<IAudioService, NAudioService>();
            builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
            builder.Services.AddSingleton<ITranscriptionLogger, FileTranscriptionLogger>();
            
            // V3.0 services
            builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();
            builder.Services.AddSingleton<IModelsCacheService, ModelsCacheService>();
            builder.Services.AddSingleton<IOpenRouterService, OpenRouterService>();
            builder.Services.AddSingleton<IOllamaService, OllamaService>();
            builder.Services.AddSingleton<IAudioNotificationService, AudioNotificationService>();
            
            // Application services
            builder.Services.AddSingleton<PushToTalkManager>();
            builder.Services.AddSingleton<TranscriptionOrchestrator>();
            
            // UI
            builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddTransient<Views.SettingsWindow>();
            builder.Services.AddTransient<Views.TranscriptionHistoryWindow>();
            
            Console.WriteLine("STARTUP: All services registered successfully");
            
            Console.WriteLine("DI TEST: Building host...");
            var host = builder.Build();
            
            Console.WriteLine("DI TEST: Getting logger...");
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("DI TEST: Logger obtained successfully");

            Console.WriteLine("DI TEST: Testing ISettingsService resolution...");
            var settingsService = host.Services.GetRequiredService<ISettingsService>();
            logger.LogInformation("DI TEST: ISettingsService resolved successfully");
            
            Console.WriteLine("DI TEST: Now testing MainWindow resolution - THIS IS THE CRITICAL TEST...");
            try
            {
                var mainWindow = host.Services.GetRequiredService<MainWindow>();
                Console.WriteLine("DI TEST: MainWindow resolved successfully!");
                logger.LogInformation("DI TEST: MainWindow resolved successfully!");
                
                Console.WriteLine("DI TEST: Creating WPF Application...");
                var app = new Application();
                
                mainWindow.Closed += (s, e) => 
                {
                    Console.WriteLine("DI TEST: Window closed, shutting down...");
                    host.Dispose();
                    app.Shutdown();
                };
                
                Console.WriteLine("DI TEST: Showing MainWindow...");
                mainWindow.Show();
                
                Console.WriteLine("DI TEST: Running application with MainWindow...");
                app.Run(mainWindow);
            }
            catch (Exception mainWindowEx)
            {
                Console.WriteLine($"DI TEST CRITICAL ERROR: MainWindow resolution failed!");
                Console.WriteLine($"DI TEST ERROR: {mainWindowEx.Message}");
                Console.WriteLine($"DI TEST STACK: {mainWindowEx.StackTrace}");
                
                var innerEx = mainWindowEx.InnerException;
                int depth = 0;
                while (innerEx != null && depth < 10)
                {
                    Console.WriteLine($"DI TEST ERROR: Inner exception {depth}: {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                    depth++;
                }
                
                Console.WriteLine("DI TEST: THIS IS LIKELY THE SOURCE OF THE STARTUP CRASH!");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("DI TEST: Application ended normally");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DI TEST ERROR: {ex.Message}");
            Console.WriteLine($"DI TEST STACK: {ex.StackTrace}");
            Console.ReadKey();
        }
    }
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
}
