using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Models;
using System.Diagnostics;
using System.Text.Json;
using System.IO;

namespace CarelessWhisperV2.Services.Python;

public class PythonEnvironmentManager
{
    private readonly ILogger<PythonEnvironmentManager> _logger;
    private readonly string _applicationDirectory;
    
    public string PythonExecutable { get; private set; } = "";
    public string PythonHome { get; private set; } = "";
    public string ScriptsDirectory { get; private set; } = "";
    public bool IsInitialized { get; private set; }

    public PythonEnvironmentManager(ILogger<PythonEnvironmentManager> logger)
    {
        _logger = logger;
        _applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Python environment (with timeout)...");

            // Use a 10-second timeout for the entire initialization process
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            try
            {
                return await InitializeInternalAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Python environment initialization timed out after 10 seconds - TTS will be unavailable");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Python environment - TTS will be unavailable");
            return false;
        }
    }

    private async Task<bool> InitializeInternalAsync(CancellationToken cancellationToken)
    {
        // First try embedded Python
        var embeddedPythonPath = Path.Combine(_applicationDirectory, "python", "python.exe");
        
        if (File.Exists(embeddedPythonPath))
        {
            PythonExecutable = embeddedPythonPath;
            PythonHome = Path.GetDirectoryName(embeddedPythonPath)!;
            ScriptsDirectory = Path.Combine(_applicationDirectory, "scripts");
            _logger.LogInformation($"Found embedded Python: {PythonExecutable}");
            
            var kittenTtsAvailable = await VerifyKittenTtsAsync(cancellationToken);
            if (kittenTtsAvailable)
            {
                IsInitialized = true;
                _logger.LogInformation("Python environment initialized successfully with embedded Python + KittenTTS");
                return true;
            }
            else
            {
                _logger.LogWarning("Embedded Python found but KittenTTS verification failed");
            }
        }
        else
        {
            _logger.LogDebug("No embedded Python found at: {Path}", embeddedPythonPath);
        }

        // Fallback to system Python
        var systemPython = await FindSystemPythonAsync(cancellationToken);
        if (!string.IsNullOrEmpty(systemPython))
        {
            PythonExecutable = systemPython;
            PythonHome = Path.GetDirectoryName(systemPython)!;
            ScriptsDirectory = Path.Combine(_applicationDirectory, "scripts");
            _logger.LogInformation($"Found system Python: {PythonExecutable}");
            
            var kittenTtsAvailable = await VerifyKittenTtsAsync(cancellationToken);
            if (kittenTtsAvailable)
            {
                IsInitialized = true;
                _logger.LogInformation("Python environment initialized successfully with system Python + KittenTTS");
                return true;
            }
            else
            {
                _logger.LogWarning("System Python found but KittenTTS verification failed");
            }
        }
        else
        {
            _logger.LogWarning("No system Python installation found");
        }

        _logger.LogWarning("Python environment initialization failed - KittenTTS will be unavailable, falling back to Windows SAPI");
        return false;
    }

    public async Task<bool> VerifyKittenTtsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var bridgeScript = Path.Combine(ScriptsDirectory, "kitten_tts_bridge.py");
            
            // Ensure bridge script exists
            if (!File.Exists(bridgeScript))
            {
                _logger.LogWarning($"Bridge script not found: {bridgeScript}");
                await CreateBridgeScriptAsync(bridgeScript);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = PythonExecutable,
                Arguments = $"\"{bridgeScript}\" --list-voices",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = ScriptsDirectory
            };

            // Use the ExecutePythonScriptAsync method which has proper timeout handling
            var result = await ExecutePythonScriptAsync(startInfo, cancellationToken);
            
            if (result.Success)
            {
                _logger.LogInformation("KittenTTS verification successful");
                return true;
            }
            else
            {
                _logger.LogError($"KittenTTS verification failed. Exit code: {result.ExitCode}, Error: {result.ErrorOutput}");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("KittenTTS verification was cancelled due to timeout");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify KittenTTS installation");
            return false;
        }
    }

    private async Task<string?> FindSystemPythonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pythonCommands = new[] { "python", "python3", "py" };

            foreach (var command in pythonCommands)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    // Use ExecutePythonScriptAsync for proper timeout handling, but set a shorter timeout for version check
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                    
                    var result = await ExecutePythonScriptAsync(startInfo, linkedCts.Token);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation($"Found system Python: {command} - {result.StandardOutput.Trim()}");
                        return command;
                    }
                    else
                    {
                        _logger.LogDebug($"Python command {command} failed: {result.ErrorOutput}");
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug($"Python command {command} timed out or was cancelled");
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, $"Failed to check Python command: {command}");
                    continue;
                }
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("System Python search was cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding system Python");
            return null;
        }
    }

    private async Task CreateBridgeScriptAsync(string scriptPath)
    {
        try
        {
            // Ensure scripts directory exists
            var scriptsDir = Path.GetDirectoryName(scriptPath)!;
            if (!Directory.Exists(scriptsDir))
            {
                Directory.CreateDirectory(scriptsDir);
            }

            var bridgeScriptContent = await GetBridgeScriptContentAsync();
            await File.WriteAllTextAsync(scriptPath, bridgeScriptContent);
            _logger.LogInformation($"Created bridge script: {scriptPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to create bridge script: {scriptPath}");
            throw;
        }
    }

    private async Task<string> GetBridgeScriptContentAsync()
    {
        try
        {
            // Try to read the bridge script from the application's scripts directory
            var sourceBridgeScript = Path.Combine(_applicationDirectory, "scripts", "kitten_tts_bridge.py");
            if (File.Exists(sourceBridgeScript))
            {
                return await File.ReadAllTextAsync(sourceBridgeScript);
            }
            
            _logger.LogWarning($"Source bridge script not found at: {sourceBridgeScript}");
            return GetFallbackBridgeScriptContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read bridge script content");
            return GetFallbackBridgeScriptContent();
        }
    }

    private string GetFallbackBridgeScriptContent()
    {
        return @"#!/usr/bin/env python3
import json
import sys

try:
    from kittentts import KittenTTS
except ImportError:
    print(json.dumps({""success"": False, ""error"": ""KittenTTS not installed""}), file=sys.stderr)
    sys.exit(1)

# Minimal fallback bridge script
print(json.dumps({""success"": True, ""voices"": [{""id"": ""expr-voice-2-f"", ""description"": ""Female Voice #2""}]}))
";
    }

    public async Task<ProcessResult> ExecutePythonScriptAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            var processTask = process.WaitForExitAsync(cancellationToken);
            
            var completedTask = await Task.WhenAny(processTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                try
                {
                    process.Kill();
                }
                catch { }
                
                return new ProcessResult
                {
                    Success = false,
                    ErrorOutput = "Process timed out after 30 seconds",
                    Duration = stopwatch.Elapsed,
                    ExitCode = -1
                };
            }
            
            var output = await outputTask;
            var error = await errorTask;
            
            return new ProcessResult
            {
                Success = process.ExitCode == 0,
                StandardOutput = output,
                ErrorOutput = error,
                Duration = stopwatch.Elapsed,
                ExitCode = process.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new ProcessResult
            {
                Success = false,
                ErrorOutput = ex.Message,
                Duration = stopwatch.Elapsed,
                ExitCode = -1
            };
        }
    }
}

public class ProcessResult
{
    public bool Success { get; set; }
    public string StandardOutput { get; set; } = "";
    public string ErrorOutput { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public int ExitCode { get; set; }
}
