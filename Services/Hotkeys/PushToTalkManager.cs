using SharpHook;
using SharpHook.Native;
using Microsoft.Extensions.Logging;

namespace CarelessWhisperV2.Services.Hotkeys;

public class PushToTalkManager : IDisposable
{
    private readonly TaskPoolGlobalHook _hook;
    private readonly KeyCode _pushToTalkKey;
    private readonly KeyCode _llmPromptKey; // NEW
    private readonly KeyCode _visionCaptureKey; // NEW - F3 for vision capture
    private readonly HashSet<KeyCode> _activeModifiers; // NEW
    private readonly ILogger<PushToTalkManager> _logger;
    private bool _isTransmitting;
    private bool _isLlmMode; // NEW
    private bool _isCopyPromptMode; // NEW for Ctrl+F2
    private bool _isVisionPttMode; // NEW for Ctrl+F3 PTT
    private readonly object _transmissionLock = new object();
    private int _hookRestartCount;
    private const int MaxRestartAttempts = 3;
    private bool _disposed;
    
    // DIAGNOSTIC: TTS trigger tracking and debouncing
    private int _ttsTriggeredCount = 0;
    private DateTime _lastTtsTriggerTime = DateTime.MinValue;
    private const int TtsDebounceMs = 200; // Prevent rapid-fire triggers within 200ms

    public event Action? TransmissionStarted;
    public event Action? TransmissionEnded;
    public event Action? LlmTransmissionStarted; // NEW
    public event Action? LlmTransmissionEnded; // NEW
    public event Action? CopyPromptTransmissionStarted; // NEW for Ctrl+F2
    public event Action? CopyPromptTransmissionEnded; // NEW for Ctrl+F2
    public event Action? VisionCaptureStarted; // NEW - Shift+F3
    public event Action? VisionCaptureEnded; // NEW - Shift+F3
    public event Action? VisionCaptureWithPromptStarted; // NEW - Ctrl+F3
    public event Action? VisionCaptureWithPromptEnded; // NEW - Ctrl+F3
    public event Action? TtsTriggered; // NEW - Ctrl+F1 for TTS

    public PushToTalkManager(
        ILogger<PushToTalkManager> logger, 
        KeyCode pushToTalkKey = KeyCode.VcF1,
        KeyCode llmPromptKey = KeyCode.VcF2,
        KeyCode visionCaptureKey = KeyCode.VcF3) // NEW
    {
        _pushToTalkKey = pushToTalkKey;
        _llmPromptKey = llmPromptKey; // NEW
        _visionCaptureKey = visionCaptureKey; // NEW
        _activeModifiers = new HashSet<KeyCode>(); // NEW
        _logger = logger;
        _hook = new TaskPoolGlobalHook();
        
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        
        // Start hook on background thread for optimal performance
        Task.Run(async () => await StartHookAsync());
    }

    private async Task StartHookAsync()
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
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        // Track modifier keys
        if (IsModifierKey(e.Data.KeyCode))
        {
            _activeModifiers.Add(e.Data.KeyCode);
            return;
        }

        // Handle Ctrl+F1 (TTS - immediate trigger) - CHECK THIS FIRST!
        if (e.Data.KeyCode == _pushToTalkKey && _activeModifiers.Contains(KeyCode.VcLeftControl))
        {
            var now = DateTime.Now;
            var timeSinceLastTrigger = now - _lastTtsTriggerTime;
            
            _ttsTriggeredCount++;
            _logger.LogInformation("DIAGNOSTIC TTS: Trigger #{Count} detected, time since last: {TimeDiff}ms", 
                _ttsTriggeredCount, timeSinceLastTrigger.TotalMilliseconds);
            
            // Debounce: Ignore triggers that occur too quickly after previous one
            if (timeSinceLastTrigger.TotalMilliseconds < TtsDebounceMs)
            {
                _logger.LogWarning("DIAGNOSTIC TTS: BLOCKED duplicate trigger #{Count} (within {DebounceMs}ms debounce window)", 
                    _ttsTriggeredCount, TtsDebounceMs);
                e.SuppressEvent = true;
                return;
            }
            
            _lastTtsTriggerTime = now;
            _logger.LogInformation("DIAGNOSTIC TTS: ACCEPTED trigger #{Count}, invoking TtsTriggered event", _ttsTriggeredCount);
            TtsTriggered?.Invoke();
            e.SuppressEvent = true;
        }
        // Handle F1 (Speech to Paste) - only when Ctrl is NOT pressed
        else if (e.Data.KeyCode == _pushToTalkKey && !_activeModifiers.Contains(KeyCode.VcLeftControl))
        {
            lock (_transmissionLock)
            {
                if (!_isTransmitting)
                {
                    _isTransmitting = true;
                    _isLlmMode = false;
                    _isCopyPromptMode = false;
                    _logger.LogDebug("Push-to-talk started (Speech to Paste)");
                    TransmissionStarted?.Invoke();
                }
            }
            e.SuppressEvent = true;
        }
        // Handle Shift+F2 (Speech-Prompt to Paste)
        else if (e.Data.KeyCode == _llmPromptKey && _activeModifiers.Contains(KeyCode.VcLeftShift))
        {
            lock (_transmissionLock)
            {
                if (!_isTransmitting)
                {
                    _isTransmitting = true;
                    _isLlmMode = true;
                    _isCopyPromptMode = false;
                    _logger.LogDebug("LLM transmission started (Speech-Prompt to Paste)");
                    LlmTransmissionStarted?.Invoke();
                }
            }
            e.SuppressEvent = true;
        }
        // Handle Ctrl+F2 (Speech Copy Prompt to Paste)
        else if (e.Data.KeyCode == _llmPromptKey && _activeModifiers.Contains(KeyCode.VcLeftControl))
        {
            lock (_transmissionLock)
            {
                if (!_isTransmitting)
                {
                    _isTransmitting = true;
                    _isLlmMode = false;
                    _isCopyPromptMode = true;
                    _logger.LogDebug("Copy-prompt transmission started (Speech Copy Prompt to Paste)");
                    CopyPromptTransmissionStarted?.Invoke();
                }
            }
            e.SuppressEvent = true;
        }
        // Handle Shift+F3 (Vision Capture - immediate trigger)
        else if (e.Data.KeyCode == _visionCaptureKey && _activeModifiers.Contains(KeyCode.VcLeftShift))
        {
            _logger.LogDebug("Vision capture triggered (Shift+F3)");
            VisionCaptureStarted?.Invoke();
            e.SuppressEvent = true;
        }
        // Handle Ctrl+F3 (Vision PTT - hold/release like other PTT keys)
        else if (e.Data.KeyCode == _visionCaptureKey && _activeModifiers.Contains(KeyCode.VcLeftControl))
        {
            lock (_transmissionLock)
            {
                if (!_isTransmitting)
                {
                    _isTransmitting = true;
                    _isLlmMode = false;
                    _isCopyPromptMode = false;
                    _isVisionPttMode = true;
                    _logger.LogDebug("Vision PTT transmission started (Ctrl+F3 hold)");
                    VisionCaptureWithPromptStarted?.Invoke();
                }
            }
            e.SuppressEvent = true;
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        // Track modifier keys
        if (IsModifierKey(e.Data.KeyCode))
        {
            _activeModifiers.Remove(e.Data.KeyCode);
            return;
        }

        // Handle key release for all modes - ensure F1 release only processes when Ctrl is not pressed
        if ((e.Data.KeyCode == _pushToTalkKey && !_isLlmMode && !_isCopyPromptMode && !_isVisionPttMode && !_activeModifiers.Contains(KeyCode.VcLeftControl)) || 
            (e.Data.KeyCode == _llmPromptKey && _isLlmMode) ||
            (e.Data.KeyCode == _llmPromptKey && _isCopyPromptMode) ||
            (e.Data.KeyCode == _visionCaptureKey && _isVisionPttMode))
        {
            lock (_transmissionLock)
            {
                if (_isTransmitting)
                {
                    _isTransmitting = false;
                    
                    if (_isLlmMode)
                    {
                        _logger.LogDebug("LLM transmission ended");
                        LlmTransmissionEnded?.Invoke();
                    }
                    else if (_isCopyPromptMode)
                    {
                        _logger.LogDebug("Copy-prompt transmission ended");
                        CopyPromptTransmissionEnded?.Invoke();
                    }
                    else if (_isVisionPttMode)
                    {
                        _isVisionPttMode = false;
                        _logger.LogDebug("Vision PTT transmission ended (Ctrl+F3 release)");
                        VisionCaptureWithPromptEnded?.Invoke();
                    }
                    else
                    {
                        _logger.LogDebug("Push-to-talk ended");
                        TransmissionEnded?.Invoke();
                    }
                }
            }
            e.SuppressEvent = true;
        }
    }

    private bool IsModifierKey(KeyCode keyCode)
    {
        return keyCode == KeyCode.VcLeftShift || 
               keyCode == KeyCode.VcRightShift ||
               keyCode == KeyCode.VcLeftControl || 
               keyCode == KeyCode.VcRightControl ||
               keyCode == KeyCode.VcLeftAlt || 
               keyCode == KeyCode.VcRightAlt;
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

    public bool IsLlmMode // NEW
    {
        get
        {
            lock (_transmissionLock)
            {
                return _isLlmMode;
            }
        }
    }

    public bool IsCopyPromptMode // NEW for Ctrl+F2
    {
        get
        {
            lock (_transmissionLock)
            {
                return _isCopyPromptMode;
            }
        }
    }

    private async Task RestartHook()
    {
        if (_hookRestartCount < MaxRestartAttempts)
        {
            _hookRestartCount++;
            await Task.Delay(1000 * _hookRestartCount); // Exponential backoff
            
            try
            {
                await _hook.RunAsync();
                _hookRestartCount = 0; // Reset on successful restart
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hook restart attempt {Attempt} failed", _hookRestartCount);
                await RestartHook();
            }
        }
        else
        {
            _logger.LogCritical("Hook restart limit exceeded");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _hook?.Dispose();
            _disposed = true;
            _logger.LogInformation("PushToTalkManager disposed");
        }
    }
}
