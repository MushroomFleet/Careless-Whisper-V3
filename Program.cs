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
using CarelessWhisperV2.Services.AudioNotification;

namespace CarelessWhisperV2;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configuration
        builder.Services.Configure<ApplicationSettings>(
            builder.Configuration.GetSection("ApplicationSettings"));
        
        // Core services
        builder.Services.AddSingleton<ISettingsService, JsonSettingsService>();
        builder.Services.AddSingleton<IClipboardService, ClipboardService>();
        builder.Services.AddSingleton<IAudioService, NAudioService>();
        builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
        builder.Services.AddSingleton<ITranscriptionLogger, FileTranscriptionLogger>();
        
        // NEW V3.0 services
        builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();
        builder.Services.AddSingleton<IOpenRouterService, OpenRouterService>();
        builder.Services.AddSingleton<IAudioNotificationService, AudioNotificationService>();
        
        // Application services
        builder.Services.AddSingleton<PushToTalkManager>();
        builder.Services.AddSingleton<TranscriptionOrchestrator>();
        
        // UI
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<Views.SettingsWindow>();
        builder.Services.AddTransient<Views.TranscriptionHistoryWindow>();
        
        // Logging
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        
        var host = builder.Build();

        // Configure unhandled exception handling
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred");
        };

        // Set STA apartment for clipboard operations
        Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

        var app = new Application();
        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        
        logger.LogInformation("Starting Careless Whisper V2");
        
        try
        {
            app.Run(mainWindow);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Application crashed");
            MessageBox.Show($"Application error: {ex.Message}", "Careless Whisper V2", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            logger.LogInformation("Application shutting down");
            host.Dispose();
        }
    }
}
