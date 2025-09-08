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
        // Console allocation - only for development or when explicitly requested
        // In production distribution builds, console should be suppressed unless --debug is passed
        bool isExplicitDebugRequest = args.Contains("--debug");
        bool isDevelopmentEnvironment = System.Diagnostics.Debugger.IsAttached;
        bool shouldShowDebug = isExplicitDebugRequest || (isDevelopmentEnvironment && !IsProductionBuild());
        bool consoleAllocated = false;
        
        if (shouldShowDebug)
        {
            try
            {
                // Try to allocate console for WPF app to see output
                if (AllocConsole())
                {
                    consoleAllocated = true;
                }
                else
                {
                    // If allocation fails, try to attach to parent console
                    consoleAllocated = AttachConsole(-1);
                }
            }
            catch (Exception)
            {
                // Ignore console allocation errors - continue without debug output
                consoleAllocated = false;
            }
        }
        
        // Use conditional console output that only writes when console is available
        WriteDebugLine("DEPENDENCY INJECTION TEST: Starting...", shouldShowDebug && consoleAllocated);
        
        try
        {
            WriteDebugLine("DI TEST: Creating host builder...", shouldShowDebug && consoleAllocated);
            var builder = Host.CreateApplicationBuilder(args);
            
            WriteDebugLine("DI TEST: Configuring logging...", shouldShowDebug && consoleAllocated);
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            
            WriteDebugLine("STARTUP: Registering all services for Careless Whisper V3...", shouldShowDebug && consoleAllocated);
            
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
            
            WriteDebugLine("STARTUP: All services registered successfully", shouldShowDebug && consoleAllocated);
            
            WriteDebugLine("DI TEST: Building host...", shouldShowDebug && consoleAllocated);
            var host = builder.Build();
            
            WriteDebugLine("DI TEST: Getting logger...", shouldShowDebug && consoleAllocated);
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("DI TEST: Logger obtained successfully");

            WriteDebugLine("DI TEST: Testing ISettingsService resolution...", shouldShowDebug && consoleAllocated);
            var settingsService = host.Services.GetRequiredService<ISettingsService>();
            logger.LogInformation("DI TEST: ISettingsService resolved successfully");
            
            WriteDebugLine("DI TEST: Now testing MainWindow resolution - THIS IS THE CRITICAL TEST...", shouldShowDebug && consoleAllocated);
            try
            {
                var mainWindow = host.Services.GetRequiredService<MainWindow>();
                WriteDebugLine("DI TEST: MainWindow resolved successfully!", shouldShowDebug && consoleAllocated);
                logger.LogInformation("DI TEST: MainWindow resolved successfully!");
                
                WriteDebugLine("DI TEST: Creating WPF Application...", shouldShowDebug && consoleAllocated);
                var app = new Application();
                
                mainWindow.Closed += (s, e) => 
                {
                    WriteDebugLine("DI TEST: Window closed, shutting down...", shouldShowDebug && consoleAllocated);
                    host.Dispose();
                    app.Shutdown();
                };
                
                WriteDebugLine("DI TEST: Showing MainWindow...", shouldShowDebug && consoleAllocated);
                mainWindow.Show();
                
                WriteDebugLine("DI TEST: Running application with MainWindow...", shouldShowDebug && consoleAllocated);
                app.Run(mainWindow);
            }
            catch (Exception mainWindowEx)
            {
                WriteDebugLine($"DI TEST CRITICAL ERROR: MainWindow resolution failed!", shouldShowDebug && consoleAllocated);
                WriteDebugLine($"DI TEST ERROR: {mainWindowEx.Message}", shouldShowDebug && consoleAllocated);
                WriteDebugLine($"DI TEST STACK: {mainWindowEx.StackTrace}", shouldShowDebug && consoleAllocated);
                
                var innerEx = mainWindowEx.InnerException;
                int depth = 0;
                while (innerEx != null && depth < 10)
                {
                    WriteDebugLine($"DI TEST ERROR: Inner exception {depth}: {innerEx.Message}", shouldShowDebug && consoleAllocated);
                    innerEx = innerEx.InnerException;
                    depth++;
                }
                
                WriteDebugLine("DI TEST: THIS IS LIKELY THE SOURCE OF THE STARTUP CRASH!", shouldShowDebug && consoleAllocated);
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.ReadKey();
                }
                return;
            }
            
            WriteDebugLine("DI TEST: Application ended normally", shouldShowDebug && consoleAllocated);
        }
        catch (Exception ex)
        {
            WriteDebugLine($"DI TEST ERROR: {ex.Message}", shouldShowDebug && consoleAllocated);
            WriteDebugLine($"DI TEST STACK: {ex.StackTrace}", shouldShowDebug && consoleAllocated);
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.ReadKey();
            }
        }
    }
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
    
    private static void WriteDebugLine(string message, bool shouldWrite = true)
    {
        // Only write to console if explicitly requested and conditions are met
        if (shouldWrite)
        {
            try
            {
                Console.WriteLine(message);
            }
            catch
            {
                // Ignore console write errors in production
            }
        }
    }
    
    private static bool IsProductionBuild()
    {
        // Detect if we're running from a distribution build
        // Production builds are typically run from dist-* directories or have specific characteristics
        var currentDirectory = System.Environment.CurrentDirectory;
        var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        
        // Check if running from distribution directories
        return currentDirectory.Contains("dist-") || 
               executablePath.Contains("dist-") ||
               !System.Diagnostics.Debugger.IsAttached;
    }
}
