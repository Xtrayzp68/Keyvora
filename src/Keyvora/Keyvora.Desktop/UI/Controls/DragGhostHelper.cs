namespace Keyvora.Desktop.UI.Controls;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;

internal sealed class DragGhostHelper : IDisposable
{
    private Window? _overlay;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Win32Point lpPoint);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point { public int X; public int Y; }

    public void StartDrag(FrameworkElement source)
    {
        var sourceRect = new Rect(new Point(0, 0), new Size(source.ActualWidth, source.ActualHeight));

        var visualBrush = new VisualBrush(source)
        {
            Stretch = Stretch.Uniform,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center,
            Viewbox = sourceRect,
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewport = sourceRect,
            ViewportUnits = BrushMappingMode.Absolute,
        };

        var scale = 1.08;

        var ghostContent = new Border
        {
            Width = source.ActualWidth,
            Height = source.ActualHeight,
            Background = visualBrush,
            CornerRadius = new CornerRadius(8),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.6,
                BlurRadius = 16,
                ShadowDepth = 4,
                Direction = 270,
                RenderingBias = RenderingBias.Performance,
            },
            RenderTransform = new ScaleTransform(scale, scale),
            RenderTransformOrigin = new Point(0.5, 0.5),
        };

        var ghostWidth = source.ActualWidth * scale;
        var ghostHeight = source.ActualHeight * scale;

        _overlay = new Window
        {
            AllowsTransparency = true,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            Topmost = true,
            Width = ghostWidth,
            Height = ghostHeight,
            Background = null,
            Content = ghostContent,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.Manual,
        };

        UpdatePosition();
        _overlay.Show();

        // Make the ghost window transparent to mouse events so OLE drag-drop
        // can reach the actual drop targets beneath it
        MakeClickThrough();
    }

    private void MakeClickThrough()
    {
        if (_overlay == null) return;
        var hwnd = new WindowInteropHelper(_overlay).Handle;
        if (hwnd == IntPtr.Zero) return;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        _ = SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
    }

    public void UpdatePosition()
    {
        if (_overlay == null) return;
        GetCursorPos(out var pt);
        _overlay.Left = pt.X - _overlay.Width / 2;
        _overlay.Top = pt.Y - _overlay.Height / 2;
    }

    public void Dispose()
    {
        _overlay?.Close();
        _overlay = null;
    }
}
