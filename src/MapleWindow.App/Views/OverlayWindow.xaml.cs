using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using MapleWindow.App.ViewModels;
using Serilog;

namespace MapleWindow.App.Views;

public partial class OverlayWindow : Window
{
    public OverlayViewModel ViewModel { get; }

    private double _workAreaBottom;

    public OverlayWindow(OverlayViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;

        // Window.Left bound via XAML did not reliably move this AllowsTransparency=True, WindowStyle=None
        // window in testing (position stuck at its initial value despite the bound property changing) —
        // setting it imperatively here is the confirmed-working alternative. CharacterRow.Height follows
        // the same imperative pattern for consistency, rather than trying a GridLength binding here.
        CharacterRow.Height = new GridLength(ViewModel.SpriteRowHeight);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // SystemParameters.WorkArea is unreliable under PerMonitorV2 DPI awareness when the process also
        // hosts WinForms (for the tray icon) — it can report a work area far too small (observed: computing
        // maxX <= 0, pinning the sprite at X=0 forever). Screen.WorkingArea (physical pixels) + this specific
        // window's actual DPI scale is robust regardless of how the WPF/WinForms DPI-awareness interaction
        // resolved at runtime.
        var dpi = VisualTreeHelper.GetDpi(this);
        var workingArea = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;

        var workAreaLeft = workingArea.Left / dpi.DpiScaleX;
        var workAreaRight = workingArea.Right / dpi.DpiScaleX;
        _workAreaBottom = workingArea.Bottom / dpi.DpiScaleY;

        Top = _workAreaBottom - Height;
        Left = workAreaLeft;

        var minX = workAreaLeft;
        var maxX = Math.Max(workAreaLeft, workAreaRight - Width);
        Log.Information(
            "OverlayWindow bounds: dpiScale=({DpiX},{DpiY}) workingArea={WorkingArea} -> minX={MinX} maxX={MaxX} top={Top}",
            dpi.DpiScaleX, dpi.DpiScaleY, workingArea, minX, maxX, Top);

        ViewModel.StartAnimation(minX, maxX);
    }

    /// <summary>
    /// SizeToContent="Height" grows/shrinks the window from Top downward by default as the speech
    /// bubble's row appears/disappears/wraps — which would drag the character (feet pinned to the
    /// bottom of its own fixed-height row) up and down with it. Re-pinning Top here every time keeps
    /// the window's bottom edge — and so the character's feet — fixed on screen; only the top edge
    /// (and the bubble above the character) moves.
    /// </summary>
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_workAreaBottom == 0) return; // not yet positioned by OnLoaded
        if (e.HeightChanged) Top = _workAreaBottom - e.NewSize.Height;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OverlayViewModel.WindowLeft))
            Left = ViewModel.WindowLeft;
        if (e.PropertyName == nameof(OverlayViewModel.SpriteRowHeight))
            CharacterRow.Height = new GridLength(ViewModel.SpriteRowHeight);
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        base.OnClosed(e);
    }
}
