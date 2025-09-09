using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Extensions.Logging;
using DrawingRectangle = System.Drawing.Rectangle;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace CarelessWhisperV2.Views;

public partial class ScreenCaptureOverlay : Window
{
    private readonly ILogger<ScreenCaptureOverlay> _logger;
    private readonly TaskCompletionSource<DrawingRectangle?> _selectionCompletionSource;
    private bool _isSelecting = false;
    private System.Windows.Point _startPoint;
    private System.Windows.Point _currentPoint;
    private Storyboard? _dashAnimation;

    public ScreenCaptureOverlay(ILogger<ScreenCaptureOverlay> logger)
    {
        _logger = logger;
        _selectionCompletionSource = new TaskCompletionSource<DrawingRectangle?>();
        
        InitializeComponent();
        InitializeOverlay();
        
        // Start dash animation
        StartDashAnimation();
        
        _logger.LogDebug("Screen capture overlay initialized");
    }

    private void InitializeOverlay()
    {
        // Set window to cover all monitors
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        
        // Focus the window to receive keyboard events
        Focusable = true;
        Focus();
        
        _logger.LogDebug("Overlay positioned: {Left},{Top} {Width}x{Height}", 
            Left, Top, Width, Height);
    }

    private void StartDashAnimation()
    {
        try
        {
            _dashAnimation = SelectionRect.Resources["DashAnimation"] as Storyboard;
            _dashAnimation?.Begin(SelectionRect);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start dash animation");
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _logger.LogDebug("Capture cancelled by user (ESC)");
                CancelSelection();
                break;
                
            case Key.Enter:
                _logger.LogDebug("Full screen capture requested (ENTER)");
                CompleteFullScreenSelection();
                break;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);
        _currentPoint = _startPoint;
        _isSelecting = true;
        
        // Hide instructions when user starts selecting
        InstructionsText.Visibility = Visibility.Collapsed;
        
        // Show and position selection rectangle
        SelectionRect.Visibility = Visibility.Visible;
        UpdateSelectionRectangle();
        
        // Capture mouse for drag operations outside window bounds
        CaptureMouse();
        
        _logger.LogDebug("Selection started at: {X},{Y}", _startPoint.X, _startPoint.Y);
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting) return;
        
        _currentPoint = e.GetPosition(this);
        UpdateSelectionRectangle();
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        
        _isSelecting = false;
        ReleaseMouseCapture();
        
        var selectionRect = GetSelectionRectangle();
        
        // Minimum selection size to prevent accidental tiny captures
        const int minSize = 10;
        if (selectionRect.Width < minSize || selectionRect.Height < minSize)
        {
            _logger.LogDebug("Selection too small ({Width}x{Height}), treating as click - using full screen", 
                selectionRect.Width, selectionRect.Height);
            CompleteFullScreenSelection();
            return;
        }
        
        _logger.LogDebug("Selection completed: {X},{Y} {Width}x{Height}", 
            selectionRect.X, selectionRect.Y, selectionRect.Width, selectionRect.Height);
        
        CompleteSelection(selectionRect);
    }

    private void UpdateSelectionRectangle()
    {
        var rect = GetSelectionRectangle();
        
        Canvas.SetLeft(SelectionRect, rect.X);
        Canvas.SetTop(SelectionRect, rect.Y);
        SelectionRect.Width = rect.Width;
        SelectionRect.Height = rect.Height;
    }

    private DrawingRectangle GetSelectionRectangle()
    {
        var x = Math.Min(_startPoint.X, _currentPoint.X);
        var y = Math.Min(_startPoint.Y, _currentPoint.Y);
        var width = Math.Abs(_currentPoint.X - _startPoint.X);
        var height = Math.Abs(_currentPoint.Y - _startPoint.Y);
        
        // Convert to screen coordinates
        var screenX = (int)(Left + x);
        var screenY = (int)(Top + y);
        
        return new DrawingRectangle(screenX, (int)screenY, (int)width, (int)height);
    }

    private void CompleteSelection(DrawingRectangle selectionRect)
    {
        _selectionCompletionSource.SetResult(selectionRect);
        Close();
    }

    private void CompleteFullScreenSelection()
    {
        var fullScreen = new DrawingRectangle(
            (int)SystemParameters.VirtualScreenLeft,
            (int)SystemParameters.VirtualScreenTop,
            (int)SystemParameters.VirtualScreenWidth,
            (int)SystemParameters.VirtualScreenHeight);
            
        _selectionCompletionSource.SetResult(fullScreen);
        Close();
    }

    private void CancelSelection()
    {
        _selectionCompletionSource.SetResult(null);
        Close();
    }

    public Task<DrawingRectangle?> GetSelectionAsync()
    {
        return _selectionCompletionSource.Task;
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            // Stop animation
            _dashAnimation?.Stop();
            
            // DO NOT auto-complete with null - only complete if not already completed
            // The TaskCompletionSource should only be completed by user actions:
            // - Mouse selection (CompleteSelection)
            // - ESC key (CancelSelection) 
            // - ENTER key (CompleteFullScreenSelection)
            _logger.LogDebug("Overlay closing. TaskCompletion status: {IsCompleted}", _selectionCompletionSource.Task.IsCompleted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during overlay cleanup");
        }
        
        base.OnClosed(e);
    }

    // Prevent window from losing focus
    protected override void OnDeactivated(EventArgs e)
    {
        Activate();
        base.OnDeactivated(e);
    }
}
