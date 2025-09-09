using System.Drawing;

namespace CarelessWhisperV2.Services.ScreenCapture;

public interface ICaptureOverlayService
{
    /// <summary>
    /// Shows the capture overlay and allows user to select an area
    /// </summary>
    /// <returns>The selected rectangle, or null if cancelled</returns>
    Task<Rectangle?> ShowCaptureOverlayAsync();
    
    /// <summary>
    /// Hides the capture overlay if currently visible
    /// </summary>
    void HideOverlay();
    
    /// <summary>
    /// Gets whether the overlay is currently visible
    /// </summary>
    bool IsOverlayVisible { get; }
    
    /// <summary>
    /// Event fired when user completes area selection
    /// </summary>
    event Action<Rectangle>? AreaSelected;
    
    /// <summary>
    /// Event fired when user cancels the capture
    /// </summary>
    event Action? CaptureCancelled;
}
