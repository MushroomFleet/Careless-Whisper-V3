using SharpHook;
using SharpHook.Native;
using Microsoft.Extensions.Logging;

namespace CarelessWhisperV2.Services.Hotkeys;

public class PushToTalkManager : IDisposable
{
    private readonly TaskPoolGlobalHook _hook;
    private readonly KeyCode _pushToTalkKey;
    private readonly KeyCode _llmPromptKey; // NEW
    private readonly HashSet<KeyCode> _activeModifiers; // NEW
    private readonly ILogger<PushToTalkManager> _logger;
    private bool _isTransmitting;
    private bool _isLlmMode; // NEW
    private bool _isCopyPromptMode; // NEW for Ctrl+F2
    private readonly object _transmissionLock = new object();
    private int _hookRestartCount;
    private const int MaxRestartAttempts = 3;
    private bool _disposed;

    public event Action? TransmissionStarted;
    public event Action? TransmissionEnded;
    public event Action? LlmTransmissionStarted; // NEW
    public event Action? LlmTransmissionEnded; // NEW
    public event Action? CopyPromptTransmissionStarted; // NEW for Ctrl+F2
    public event Action? CopyPromptTransmissionEnded; // NEW for Ctrl+F2

    public PushToTalkManager(
        ILogger<PushToTalkManager> logger, 
        KeyCode pushToTalkKey = KeyCode.VcF1,
        KeyCode llmPromptKey = KeyCode.VcF2) // NEW
    {
        _pushToTalkKey = pushToTalkKey;
        _llmPromptKey = llmPromptKey; // NEW
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

        // Handle F1 (Speech to Paste)
        if (e.Data.KeyCode == _pushToTalkKey)
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
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        // Track modifier keys
        if (IsModifierKey(e.Data.KeyCode))
        {
            _activeModifiers.Remove(e.Data.KeyCode);
            return;
        }

        // Handle key release for all modes
        if ((e.Data.KeyCode == _pushToTalkKey && !_isLlmMode && !_isCopyPromptMode) || 
            (e.Data.KeyCode == _llmPromptKey && _isLlmMode) ||
            (e.Data.KeyCode == _llmPromptKey && _isCopyPromptMode))
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
