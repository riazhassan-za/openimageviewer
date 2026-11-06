using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace OpenImageViewer;

public partial class MainWindow : Window
{
    private static readonly string[] Extensions =
        [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".ico", ".webp", ".heic"];

    private const double ZoomStep = 1.25, MinZoom = 0.1, MaxZoom = 50;

    private List<string> _files = [];
    private int _index = -1;
    private BitmapImage? _bitmap;

    private bool _dragging;
    private Point _dragStart, _translateStart;

    public MainWindow(string? path)
    {
        InitializeComponent();
        ImageView.SizeChanged += (_, _) => UpdateStatus();
        if (path != null && File.Exists(path)) OpenFile(path);
    }

    // ---- Files / navigation ----

    private void OpenFile(string path)
    {
        path = Path.GetFullPath(path);
        _files = Directory.EnumerateFiles(Path.GetDirectoryName(path)!)
            .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _index = _files.FindIndex(f => string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
        if (_index < 0) { _files.Insert(0, path); _index = 0; }
        ShowCurrent();
    }

    private void Step(int delta)
    {
        if (_files.Count == 0) return;
        _index = (_index + delta + _files.Count) % _files.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var path = _files[_index];
        Title = $"{Path.GetFileName(path)} - OpenImageViewer";
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // don't keep the file locked
            bmp.UriSource = new Uri(path);
            bmp.EndInit();
            bmp.Freeze();
            _bitmap = bmp;
            ImageView.Source = bmp;
            // Fit large images to the window, but show small ones at native pixel size.
            ImageView.MaxWidth = bmp.PixelWidth;
            ImageView.MaxHeight = bmp.PixelHeight;
            HintText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _bitmap = null;
            ImageView.Source = null;
            HintText.Text = $"Cannot open {Path.GetFileName(path)}\n{ex.Message}";
            HintText.Visibility = Visibility.Visible;
        }
        ResetView();
    }

    // ---- Zoom / pan ----

    private void ResetView()
    {
        Scale.ScaleX = Scale.ScaleY = 1;
        Translate.X = Translate.Y = 0;
        UpdateStatus();
    }

    // Zoom so the image point under 'anchor' (in ImageView's untransformed coords) stays put.
    private void ZoomAt(Point anchor, double factor)
    {
        if (_bitmap == null) return;
        double old = Scale.ScaleX;
        double next = Math.Clamp(old * factor, MinZoom, MaxZoom);
        Translate.X -= anchor.X * (next - old);
        Translate.Y -= anchor.Y * (next - old);
        Scale.ScaleX = Scale.ScaleY = next;
        UpdateStatus();
    }

    private void ZoomAtCenter(double factor)
    {
        var center = new Point(Viewport.ActualWidth / 2, Viewport.ActualHeight / 2);
        ZoomAt(Viewport.TranslatePoint(center, ImageView), factor);
    }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e) =>
        ZoomAt(e.GetPosition(ImageView), e.Delta > 0 ? ZoomStep : 1 / ZoomStep);

    private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_bitmap == null || Scale.ScaleX <= 1) return; // pan only when zoomed in
        _dragging = true;
        _dragStart = e.GetPosition(Viewport);
        _translateStart = new Point(Translate.X, Translate.Y);
        Viewport.CaptureMouse();
        Viewport.Cursor = Cursors.SizeAll;
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var p = e.GetPosition(Viewport);
        Translate.X = _translateStart.X + (p.X - _dragStart.X);
        Translate.Y = _translateStart.Y + (p.Y - _dragStart.Y);
    }

    private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        Viewport.ReleaseMouseCapture();
        Viewport.Cursor = null;
    }

    private void UpdateStatus()
    {
        if (_index < 0) { StatusText.Text = PathText.Text = ""; return; }
        PathText.Text = Path.GetDirectoryName(_files[_index]);
        var text = $"{_index + 1} / {_files.Count}";
        if (_bitmap != null)
        {
            double zoom = ImageView.ActualWidth * Scale.ScaleX / _bitmap.PixelWidth;
            text += $"   {_bitmap.PixelWidth} × {_bitmap.PixelHeight}   {zoom:P0}";
        }
        StatusText.Text = text;
    }

    // ---- Toolbar / keyboard ----

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Images|" + string.Join(";", Extensions.Select(x => "*" + x)) + "|All files|*.*",
            InitialDirectory = _index >= 0 ? Path.GetDirectoryName(_files[_index]) : null
        };
        if (dlg.ShowDialog(this) == true) OpenFile(dlg.FileName);
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths && File.Exists(paths[0]))
            OpenFile(paths[0]);
    }

    private void Prev_Click(object sender, RoutedEventArgs e) => Step(-1);
    private void Next_Click(object sender, RoutedEventArgs e) => Step(1);
    private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomAtCenter(ZoomStep);
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomAtCenter(1 / ZoomStep);
    private void Fit_Click(object sender, RoutedEventArgs e) => ResetView();
    private void Fullscreen_Click(object sender, RoutedEventArgs e) => SetFullscreen(!_fullscreen);
    private void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();

    private bool _fullscreen;
    private WindowState _restoreState;

    private void SetFullscreen(bool on)
    {
        if (on == _fullscreen) return;
        _fullscreen = on;
        if (on)
        {
            _restoreState = WindowState;
            WindowState = WindowState.Normal; // required so Maximized covers the taskbar
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = _restoreState;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left or Key.Up: Step(-1); e.Handled = true; break;
            case Key.Right or Key.Down or Key.Space: Step(1); e.Handled = true; break;
            case Key.F11: SetFullscreen(!_fullscreen); e.Handled = true; break;
            case Key.Escape when _fullscreen: SetFullscreen(false); e.Handled = true; break;
            case Key.O when Keyboard.Modifiers == ModifierKeys.Control:
                Open_Click(this, e); e.Handled = true; break;
        }
    }
}
