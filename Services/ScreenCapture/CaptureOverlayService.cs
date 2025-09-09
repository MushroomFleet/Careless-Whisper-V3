using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CarelessWhisperV2.Views;

namespace CarelessWhisperV2.Services.ScreenCapture;

public class CaptureOverlayService : ICaptureOverlayService
{
    private readonly ILogger<CaptureOverlayService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private ScreenCaptureOverlay? _currentOverlay;

    public CaptureOverlayService(ILogger<CaptureOverlayService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public bool IsOverlayVisible => _currentOverlay != null && _currentOverlay.IsVisible;

    public event Action<Rectangle>? AreaSelected;
    public event Action? CaptureCancelled;

    public async Task<Rectangle?> ShowCaptureOverlayAsync()
    {
        try
        {
            _logger.LogInformation("Showing screen capture overlay");

            if (_currentOverlay != null)
            {
                _logger.LogWarning("Overlay already visible, hiding previous instance");
                HideOverlay();
            }

            // Create and show overlay on UI thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var logger = _serviceProvider.GetService<ILogger<ScreenCaptureOverlay>>();
                    _currentOverlay = new ScreenCaptureOverlay(logger!);
                    _currentOverlay.Show();
                    _logger.LogInformation("Overlay created and shown successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create and show overlay");
                    throw;
                }
            });

            if (_currentOverlay == null)
            {
                throw new InvalidOperationException("Failed to create overlay");
            }

            // Wait for user selection (this happens outside the Dispatcher context)
            _logger.LogInformation("Waiting for user selection...");
            var selectedArea = await _currentOverlay.GetSelectionAsync();
            
            _logger.LogInformation("User selection completed: {Area}", selectedArea);

            // Clean up overlay on UI thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_currentOverlay != null)
                {
                    _currentOverlay.Close();
                    _currentOverlay = null;
                }
            });

            // Fire events based on result
            if (selectedArea.HasValue)
            {
                AreaSelected?.Invoke(selectedArea.Value);
            }
            else
            {
                CaptureCancelled?.Invoke();
            }

            return selectedArea;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show capture overlay");
            HideOverlay(); // Ensure cleanup on error
            throw;
        }
    }

    public void HideOverlay()
    {
        try
        {
            if (_currentOverlay != null)
            {
                _logger.LogInformation("Hiding screen capture overlay");
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _currentOverlay?.Close();
                });
                
                _currentOverlay = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding capture overlay");
        }
    }
}
