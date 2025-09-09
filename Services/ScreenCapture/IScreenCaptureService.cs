using System.Drawing;

namespace CarelessWhisperV2.Services.ScreenCapture;

public interface IScreenCaptureService
{
    /// <summary>
    /// Captures the screen within the specified bounds
    /// </summary>
    /// <param name="bounds">The rectangular area to capture</param>
    /// <returns>The captured image as a Bitmap</returns>
    Task<Bitmap> CaptureScreenAsync(Rectangle bounds);
    
    /// <summary>
    /// Captures the full screen
    /// </summary>
    /// <returns>The captured image as a Bitmap</returns>
    Task<Bitmap> CaptureFullScreenAsync();
    
    /// <summary>
    /// Gets the virtual screen bounds (all monitors combined)
    /// </summary>
    /// <returns>Rectangle representing the virtual screen area</returns>
    Rectangle GetVirtualScreenBounds();
    
    /// <summary>
    /// Checks if the modern Windows.Graphics.Capture API is available
    /// </summary>
    /// <returns>True if modern capture API is supported</returns>
    bool IsModernCaptureSupported();
}
