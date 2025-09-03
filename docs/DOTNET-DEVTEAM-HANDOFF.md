# Building a Production-Ready .NET 8.0 Push-to-Talk Desktop Application

Building a sophisticated system tray application that captures audio via global hotkeys and transcribes speech using Whisper requires careful orchestration of multiple technologies. Based on comprehensive research of current .NET 8.0 capabilities, this guide provides battle-tested implementations, proven library choices, and production patterns that handle real-world edge cases.

The key insight is that success depends on choosing the right libraries for each component: **H.NotifyIcon** for modern system tray functionality, **SharpHook** for robust global hotkeys, **NAudio** for reliable audio capture, and **Whisper.NET** for local speech-to-text processing. Each component requires specific threading, error handling, and resource management strategies to achieve production reliability.

## System tray implementation with H.NotifyIcon

The traditional `System.Windows.Forms.NotifyIcon` has been superseded by **H.NotifyIcon**, which offers superior .NET 8.0 compatibility, Windows 11 Efficiency Mode support, and modern development patterns. This library handles the complexities of tray-only applications while providing rich context menu functionality.

### Core implementation pattern

```csharp
// Install: H.NotifyIcon.Wpf v2.3.0
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Configure for tray-only operation
        this.WindowState = WindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Hide();
        
        // Enable Windows 11 Efficiency Mode for background operation
        this.Hide(enableEfficiencyMode: true);
    }

    private void ShowApplication_Click(object sender, RoutedEventArgs e)
    {
        this.Show(disableEfficiencyMode: true);
        this.WindowState = WindowState.Normal;
        this.ShowInTaskbar = true;
        this.Activate();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            this.Hide(enableEfficiencyMode: true);
        }
        base.OnStateChanged(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Prevent actual closing - minimize to tray instead
        e.Cancel = true;
        this.Hide(enableEfficiencyMode: true);
    }
}
```

The **Windows 11 Efficiency Mode** integration automatically reduces resource consumption when the application is hidden, making it ideal for always-running system tray applications. The lifecycle management ensures proper startup behavior and prevents accidental application termination.

### Advanced context menu implementation

```xml
<Window xmlns:tb="clr-namespace:H.NotifyIcon;assembly=H.NotifyIcon.Wpf">
    <Window.Resources>
        <ContextMenu x:Key="TrayMenu">
            <MenuItem Header="Start Recording" Click="StartRecording_Click"/>
            <MenuItem Header="Settings" Click="Settings_Click"/>
            <Separator/>
            <MenuItem Header="Exit" Click="Exit_Click"/>
        </ContextMenu>
    </Window.Resources>
    
    <tb:TaskbarIcon 
        x:Name="TrayIcon"
        IconSource="/Assets/app-icon.ico"
        ToolTipText="Push-to-Talk Transcriber"
        ContextMenu="{StaticResource TrayMenu}"
        MenuActivation="RightClick"
        TrayLeftMouseUp="TrayIcon_LeftMouseUp"/>
</Window>
```

## Global hotkey detection with SharpHook

**SharpHook v5.3.8** provides the most robust global hotkey implementation for .NET 8.0, offering cross-platform compatibility, high performance, and thread-safe operation. Unlike older Win32 API approaches, SharpHook handles hotkey conflicts gracefully and provides comprehensive error recovery.

### Push-to-talk implementation pattern

```csharp
// Install: SharpHook v5.3.8
using SharpHook;
using SharpHook.Native;

public class PushToTalkManager : IDisposable
{
    private readonly TaskPoolGlobalHook _hook;
    private readonly KeyCode _pushToTalkKey;
    private bool _isTransmitting;
    private readonly object _transmissionLock = new object();

    public event Action TransmissionStarted;
    public event Action TransmissionEnded;

    public PushToTalkManager(KeyCode pushToTalkKey = KeyCode.VcF1)
    {
        _pushToTalkKey = pushToTalkKey;
        _hook = new TaskPoolGlobalHook();
        
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        
        // Start hook on background thread for optimal performance
        Task.Run(async () => await _hook.RunAsync());
    }

    private void OnKeyPressed(object sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode == _pushToTalkKey)
        {
            lock (_transmissionLock)
            {
                if (!_isTransmitting)
                {
                    _isTransmitting = true;
                    TransmissionStarted?.Invoke();
                }
            }
            
            // Suppress key to prevent interference with other applications
            e.SuppressEvent = true;
        }
    }

    private void OnKeyReleased(object sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode == _pushToTalkKey)
        {
            lock (_transmissionLock)
            {
                if (_isTransmitting)
                {
                    _isTransmitting = false;
                    TransmissionEnded?.Invoke();
                }
            }
            
            e.SuppressEvent = true;
        }
    }

    public bool IsTransmitting
    {
        get
        {
            lock (_transmissionLock)
            {
                return _isTransmitting;
            }
        }
    }

    public void Dispose()
    {
        _hook?.Dispose();
    }
}
```

The **thread-safe design** with lock-based state management prevents race conditions during rapid key presses. The `TaskPoolGlobalHook` provides better performance than `SimpleGlobalHook` for applications that need to respond to other system events simultaneously.

### Robust error handling for global hooks

```csharp
public class RobustHotkeyManager : IDisposable
{
    private readonly ILogger _logger;
    private TaskPoolGlobalHook _hook;
    private int _hookRestartCount;
    private const int MaxRestartAttempts = 3;

    public RobustHotkeyManager(ILogger logger)
    {
        _logger = logger;
        InitializeHook();
    }

    private void InitializeHook()
    {
        try
        {
            _hook?.Dispose();
            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            
            Task.Run(async () =>
            {
                try
                {
                    await _hook.RunAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hook failed, attempting restart");
                    await RestartHook();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize hook");
        }
    }

    private async Task RestartHook()
    {
        if (_hookRestartCount < MaxRestartAttempts)
        {
            _hookRestartCount++;
            await Task.Delay(1000 * _hookRestartCount); // Exponential backoff
            InitializeHook();
        }
        else
        {
            _logger.LogCritical("Hook restart limit exceeded");
        }
    }
}
```

This **automatic recovery mechanism** handles temporary hook failures that can occur when other applications interfere with the global hook chain or during system suspend/resume cycles.

## Audio recording with NAudio

**NAudio 2.2.1** remains the most mature and reliable audio recording solution for .NET 8.0, offering comprehensive WASAPI support, excellent documentation, and production-proven stability. The library's modular architecture allows precise control over audio capture parameters critical for speech recognition.

### Whisper-optimized audio capture

```csharp
// Install: NAudio v2.2.1
using NAudio.Wave;
using NAudio.CoreAudioApi;

public class WhisperAudioRecorder : IDisposable
{
    private WasapiCapture _capture;
    private WaveFileWriter _writer;
    private readonly object _lockObject = new object();
    private bool _disposed = false;

    // Whisper-optimal format: 16kHz, 16-bit, mono
    public static readonly WaveFormat WhisperFormat = new WaveFormat(16000, 16, 1);

    public void StartRecording(string outputPath)
    {
        lock (_lockObject)
        {
            if (_capture != null || _disposed) return;

            // Use WASAPI for modern Windows audio capture
            _capture = new WasapiCapture
            {
                WaveFormat = WhisperFormat
            };

            _writer = new WaveFileWriter(outputPath, WhisperFormat);

            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;

            _capture.StartRecording();
        }
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        lock (_lockObject)
        {
            if (_writer != null && !_disposed)
            {
                _writer.Write(e.Buffer, 0, e.BytesRecorded);
                _writer.Flush(); // Ensure immediate write for real-time processing
            }
        }
    }

    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        lock (_lockObject)
        {
            _writer?.Dispose();
            _writer = null;
        }

        if (e.Exception != null)
        {
            // Log recording errors
            Console.WriteLine($"Recording stopped due to error: {e.Exception.Message}");
        }
    }

    public void StopRecording()
    {
        lock (_lockObject)
        {
            _capture?.StopRecording();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lockObject)
        {
            _capture?.Dispose();
            _writer?.Dispose();
            _disposed = true;
        }
    }
}
```

The **16kHz sample rate** provides the optimal balance between file size and speech recognition accuracy. WASAPI offers lower latency and better resource efficiency compared to the older WaveIn API, crucial for responsive push-to-talk operation.

### Production-ready audio device management

```csharp
public class AudioDeviceManager
{
    public static List<AudioDevice> GetAvailableMicrophones()
    {
        var devices = new List<AudioDevice>();
        
        using var enumerator = new MMDeviceEnumerator();
        var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        
        foreach (var endpoint in endpoints)
        {
            devices.Add(new AudioDevice
            {
                Id = endpoint.ID,
                Name = endpoint.FriendlyName,
                IsDefault = endpoint.ID == enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia).ID
            });
        }

        return devices;
    }

    public static WasapiCapture CreateCaptureDevice(string deviceId = null)
    {
        using var enumerator = new MMDeviceEnumerator();
        
        var device = string.IsNullOrEmpty(deviceId) 
            ? enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia)
            : enumerator.GetDevice(deviceId);
        
        if (device.State != DeviceState.Active)
        {
            throw new InvalidOperationException("Selected audio device is not active");
        }

        return new WasapiCapture(device)
        {
            WaveFormat = WhisperAudioRecorder.WhisperFormat
        };
    }
}

public class AudioDevice
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsDefault { get; set; }
}
```

This **device enumeration system** handles dynamic audio device changes, which commonly occur when users plug/unplug USB microphones or Bluetooth headsets during application runtime.

## Whisper integration with Whisper.NET

**Whisper.NET v1.8.1** provides the most efficient local speech-to-text processing for .NET applications, built on the high-performance whisper.cpp implementation. The library supports GPU acceleration and offers significantly better performance than Python-based alternatives.

### Local model management and transcription

```csharp
// Install: Whisper.net v1.8.1, Whisper.net.Runtime v1.8.1
using Whisper.net;
using Whisper.net.Ggml;

public class WhisperTranscriptionService : IDisposable
{
    private readonly string _modelPath;
    private readonly WhisperFactory _whisperFactory;
    private readonly ILogger _logger;

    public async Task<WhisperTranscriptionService> CreateAsync(
        GgmlType modelType = GgmlType.Base, 
        ILogger logger = null)
    {
        var service = new WhisperTranscriptionService(logger);
        await service.InitializeAsync(modelType);
        return service;
    }

    private WhisperTranscriptionService(ILogger logger = null)
    {
        _logger = logger;
    }

    private async Task InitializeAsync(GgmlType modelType)
    {
        _modelPath = $"ggml-{modelType.ToString().ToLower()}.bin";
        
        if (!File.Exists(_modelPath))
        {
            _logger?.LogInformation("Downloading Whisper model: {ModelType}", modelType);
            await DownloadModelAsync(modelType);
        }
        
        _whisperFactory = WhisperFactory.FromPath(_modelPath);
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath, 
        CancellationToken cancellationToken = default)
    {
        using var processor = _whisperFactory.CreateBuilder()
            .WithLanguage("auto")
            .WithThreads(Environment.ProcessorCount)
            .WithSpeedup(true) // Enable optimizations
            .Build();
        
        using var fileStream = File.OpenRead(audioFilePath);
        var segments = new List<TranscriptionSegment>();

        await foreach (var result in processor.ProcessAsync(fileStream, cancellationToken))
        {
            segments.Add(new TranscriptionSegment
            {
                Start = result.Start,
                End = result.End,
                Text = result.Text.Trim()
            });
        }

        return new TranscriptionResult
        {
            Segments = segments,
            FullText = string.Join(" ", segments.Select(s => s.Text)),
            Language = "auto" // Could detect language from processor
        };
    }

    private async Task DownloadModelAsync(GgmlType modelType)
    {
        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType);
        using var fileWriter = File.OpenWrite(_modelPath);
        await modelStream.CopyToAsync(fileWriter);
    }

    public void Dispose()
    {
        _whisperFactory?.Dispose();
    }
}

public class TranscriptionResult
{
    public List<TranscriptionSegment> Segments { get; set; } = new();
    public string FullText { get; set; } = "";
    public string Language { get; set; } = "";
}

public class TranscriptionSegment
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Text { get; set; } = "";
}
```

The **automatic model downloading** ensures the application works out-of-the-box, while the segmented transcription results provide timing information that can be useful for advanced features like keyword highlighting or playback synchronization.

### GPU acceleration configuration

```csharp
// Configure GPU acceleration for optimal performance
RuntimeOptions.RuntimeLibraryOrder = new[]
{
    RuntimeLibrary.Cuda,      // NVIDIA GPUs (20-30x faster)
    RuntimeLibrary.Vulkan,    // Windows with Vulkan support
    RuntimeLibrary.CoreML,    // Apple Silicon Macs
    RuntimeLibrary.OpenVino,  // Intel hardware acceleration
    RuntimeLibrary.Cpu        // CPU fallback
};

// Install additional runtime packages for GPU support:
// Whisper.net.Runtime.Cuda (requires CUDA 12.1+)
// Whisper.net.Runtime.Vulkan (Windows x64)
```

**Model size recommendations** for different use cases:
- **Tiny (39M parameters)**: Testing and development, ~1GB RAM
- **Base (74M parameters)**: General purpose, balanced performance, ~1GB RAM  
- **Small (244M parameters)**: Production recommended, good accuracy, ~2GB RAM
- **Medium (769M parameters)**: High accuracy requirements, ~5GB RAM
- **Large (1550M parameters)**: Maximum accuracy, professional use, ~10GB RAM

## Windows clipboard integration

Windows clipboard operations in .NET 8.0 require careful thread management due to STA (Single-Thread Apartment) requirements. The implementation must handle clipboard access failures gracefully, as other applications can lock clipboard access temporarily.

### Thread-safe clipboard service

```csharp
public interface IClipboardService
{
    Task<string> GetTextAsync();
    Task SetTextAsync(string text);
    Task<bool> ContainsTextAsync();
}

public class ClipboardService : IClipboardService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<string> GetTextAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    throw new InvalidOperationException("Clipboard operations require STA thread");
                }

                return Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
            });
        }
        catch (ExternalException ex)
        {
            // Handle clipboard access failures (common when other apps lock clipboard)
            throw new ClipboardException($"Failed to access clipboard: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        await _semaphore.WaitAsync();
        try
        {
            await Task.Run(() =>
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    throw new InvalidOperationException("Clipboard operations require STA thread");
                }

                // Retry logic for clipboard access failures
                var attempts = 0;
                while (attempts < 3)
                {
                    try
                    {
                        Clipboard.SetText(text);
                        return;
                    }
                    catch (ExternalException) when (attempts < 2)
                    {
                        attempts++;
                        Thread.Sleep(100); // Brief delay before retry
                    }
                }
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ContainsTextAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await Task.Run(() => Clipboard.ContainsText());
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public class ClipboardException : Exception
{
    public ClipboardException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

The **semaphore-based synchronization** prevents concurrent clipboard access while maintaining responsiveness. The retry mechanism handles transient clipboard lock situations that commonly occur in multi-application environments.

## Settings management and hotkey customization

Modern .NET 8.0 applications should leverage the built-in configuration system with the Options pattern for type-safe settings management. This approach provides validation, change notification, and seamless integration with dependency injection.

### Configuration architecture

```csharp
// Application settings model
public class ApplicationSettings : IValidatableObject
{
    public string Theme { get; set; } = "Dark";
    public bool AutoStartWithWindows { get; set; } = false;
    public HotkeySettings Hotkeys { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public WhisperSettings Whisper { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Hotkeys == null)
            yield return new ValidationResult("Hotkeys configuration is required");
        
        if (Audio == null)
            yield return new ValidationResult("Audio configuration is required");
    }
}

public class HotkeySettings
{
    public string PushToTalkKey { get; set; } = "F1";
    public bool RequireModifiers { get; set; } = false;
    public List<string> Modifiers { get; set; } = new();
}

public class AudioSettings
{
    public string PreferredDeviceId { get; set; } = "";
    public int SampleRate { get; set; } = 16000;
    public int BufferSize { get; set; } = 1024;
}

public class WhisperSettings
{
    public string ModelSize { get; set; } = "Base";
    public bool EnableGpuAcceleration { get; set; } = true;
    public string Language { get; set; } = "auto";
}
```

### Settings persistence service

```csharp
public interface ISettingsService
{
    Task<T> LoadSettingsAsync<T>() where T : new();
    Task SaveSettingsAsync<T>(T settings);
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}

public class JsonSettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<JsonSettingsService> _logger;

    public JsonSettingsService(ILogger<JsonSettingsService> logger)
    {
        _logger = logger;
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "PushToTalkTranscriber");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<T> LoadSettingsAsync<T>() where T : new()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                var defaultSettings = new T();
                await SaveSettingsAsync(defaultSettings);
                return defaultSettings;
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            });

            return settings ?? new T();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            return new T();
        }
    }

    public async Task SaveSettingsAsync<T>(T settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_settingsPath, json);
            
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(typeof(T), settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}

public class SettingsChangedEventArgs : EventArgs
{
    public Type SettingsType { get; }
    public object Settings { get; }

    public SettingsChangedEventArgs(Type settingsType, object settings)
    {
        SettingsType = settingsType;
        Settings = settings;
    }
}
```

### Hotkey configuration UI

```csharp
public partial class HotkeyControl : UserControl
{
    public static readonly DependencyProperty HotkeyProperty =
        DependencyProperty.Register(nameof(Hotkey), typeof(string), typeof(HotkeyControl));
    
    public string Hotkey
    {
        get => (string)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        
        var modifiers = new List<string>();
        
        // Capture modifier keys
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            modifiers.Add("Ctrl");
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            modifiers.Add("Alt");
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            modifiers.Add("Shift");
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            modifiers.Add("Win");
            
        // Capture main key (ignore modifier keys themselves)
        if (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl &&
            e.Key != Key.LeftAlt && e.Key != Key.RightAlt &&
            e.Key != Key.LeftShift && e.Key != Key.RightShift &&
            e.Key != Key.LWin && e.Key != Key.RWin)
        {
            modifiers.Add(e.Key.ToString());
            Hotkey = string.Join("+", modifiers);
            
            // Update the TextBox display
            if (sender is TextBox textBox)
            {
                textBox.Text = Hotkey;
            }
        }
    }
}
```

## Production-ready project architecture

A maintainable .NET 8.0 desktop application requires clean separation of concerns, dependency injection, and comprehensive error handling. The recommended architecture follows Domain-Driven Design principles while leveraging .NET's built-in hosting and configuration systems.

### Project structure and dependency injection

```csharp
// Program.cs - Application bootstrapping
[STAThread]
public static void Main(string[] args)
{
    var host = Host.CreateApplicationBuilder(args)
        .ConfigureServices((context, services) =>
        {
            // Configuration
            services.Configure<ApplicationSettings>(
                context.Configuration.GetSection("ApplicationSettings"));
            
            // Core services
            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IAudioService, NAudioService>();
            services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
            
            // Application services
            services.AddSingleton<PushToTalkManager>();
            services.AddSingleton<AudioRecordingService>();
            services.AddSingleton<TranscriptionOrchestrator>();
            
            // UI
            services.AddSingleton<MainWindow>();
            services.AddTransient<SettingsWindow>();
            
            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.AddEventLog(); // For production monitoring
            });
        })
        .Build();

    // Configure unhandled exception handling
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    {
        logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred");
    };

    var app = new Application();
    var mainWindow = host.Services.GetRequiredService<MainWindow>();
    
    app.Run(mainWindow);
}
```

### Orchestration service for complete workflow

```csharp
public class TranscriptionOrchestrator : IDisposable
{
    private readonly PushToTalkManager _hotkeyManager;
    private readonly AudioRecordingService _audioService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<TranscriptionOrchestrator> _logger;
    
    private string _currentRecordingPath;
    private bool _disposed;

    public TranscriptionOrchestrator(
        PushToTalkManager hotkeyManager,
        AudioRecordingService audioService,
        ITranscriptionService transcriptionService,
        IClipboardService clipboardService,
        ILogger<TranscriptionOrchestrator> logger)
    {
        _hotkeyManager = hotkeyManager;
        _audioService = audioService;
        _transcriptionService = transcriptionService;
        _clipboardService = clipboardService;
        _logger = logger;

        _hotkeyManager.TransmissionStarted += OnTransmissionStarted;
        _hotkeyManager.TransmissionEnded += OnTransmissionEnded;
    }

    private async void OnTransmissionStarted()
    {
        try
        {
            _currentRecordingPath = GenerateRecordingPath();
            await _audioService.StartRecordingAsync(_currentRecordingPath);
            _logger.LogInformation("Recording started: {Path}", _currentRecordingPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording");
        }
    }

    private async void OnTransmissionEnded()
    {
        try
        {
            await _audioService.StopRecordingAsync();
            _logger.LogInformation("Recording stopped: {Path}", _currentRecordingPath);

            if (File.Exists(_currentRecordingPath))
            {
                // Process transcription in background
                _ = Task.Run(async () => await ProcessTranscriptionAsync(_currentRecordingPath));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop recording");
        }
    }

    private async Task ProcessTranscriptionAsync(string audioFilePath)
    {
        try
        {
            _logger.LogInformation("Starting transcription: {Path}", audioFilePath);
            
            var result = await _transcriptionService.TranscribeAsync(audioFilePath);
            
            if (!string.IsNullOrWhiteSpace(result.FullText))
            {
                await _clipboardService.SetTextAsync(result.FullText);
                _logger.LogInformation("Transcription completed: {Text}", result.FullText);
            }
            else
            {
                _logger.LogWarning("Transcription returned empty result");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed: {Path}", audioFilePath);
        }
        finally
        {
            // Clean up temporary audio file
            try
            {
                File.Delete(audioFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary file: {Path}", audioFilePath);
            }
        }
    }

    private string GenerateRecordingPath()
    {
        var tempPath = Path.GetTempPath();
        var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav";
        return Path.Combine(tempPath, fileName);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _hotkeyManager?.Dispose();
            _audioService?.Dispose();
            _transcriptionService?.Dispose();
            _disposed = true;
        }
    }
}
```

## Critical production considerations

**Resource Management**: Always implement proper disposal patterns and use `using` statements for temporary resources. Audio and Whisper components consume significant memory that must be released promptly.

**Threading**: Maintain strict thread separation - UI operations on the main thread, global hooks on background threads, and I/O operations on the thread pool. Cross-thread operations must use proper synchronization.

**Error Recovery**: Implement exponential backoff retry logic for transient failures. Global hooks can fail during system events, audio devices can be disconnected, and clipboard access can be temporarily blocked.

**Performance Monitoring**: Log memory usage patterns, especially around Whisper model loading and audio buffer management. Consider implementing automatic garbage collection triggers after resource-intensive operations.

**User Experience**: Provide visual feedback for all operations - recording status in the system tray icon, progress indicators for transcription, and clear error messages for failures.

This architecture provides a solid foundation for a production-ready push-to-talk transcription application that handles real-world usage scenarios reliably while maintaining good performance and user experience.

## Essential NuGet packages

```xml
<PackageReference Include="H.NotifyIcon.Wpf" Version="2.3.0" />
<PackageReference Include="SharpHook" Version="5.3.8" />
<PackageReference Include="NAudio" Version="2.2.1" />
<PackageReference Include="Whisper.net" Version="1.8.1" />
<PackageReference Include="Whisper.net.Runtime" Version="1.8.1" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
```

The combination of these carefully selected libraries and architectural patterns creates a robust, maintainable desktop application capable of handling the complexities of real-time audio processing and speech recognition in a production environment.