using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RCS.ServerApp.Services;
using RCS.ServerApp.ViewModels;
using RCS.Shared.Models;
using WpfMouseButton = System.Windows.Input.MouseButton;

namespace RCS.ServerApp.Views;

/// <summary>
/// Interaction logic for RemoteViewWindow.xaml
/// </summary>
public partial class RemoteViewWindow : Window
{
    private readonly RemoteViewViewModel _viewModel;
    private bool _isMouseDown = false;
    private Point _lastMousePosition;
    private bool _isFullscreen = false;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private double _currentZoom = 1.0;
    private ScaleTransform _zoomTransform;

    public RemoteViewWindow(
        ClientViewModel client, 
        ClientConnection connection, 
        ScreenReceiver screenReceiver, 
        ControlSender controlSender)
    {
        InitializeComponent();
        
        _viewModel = new RemoteViewViewModel(client, connection, screenReceiver, controlSender);
        DataContext = _viewModel;

        // Zoom transform oluştur
        _zoomTransform = new ScaleTransform(_currentZoom, _currentZoom);
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_zoomTransform);
        RemoteImageControl.RenderTransform = transformGroup;
        RemoteImageControl.RenderTransformOrigin = new Point(0.5, 0.5);

        // Screen packet event handler
        connection.ScreenPacketReceived += OnScreenPacketReceived;
        
        // Window kapanırken temizlik
        Closed += (s, e) =>
        {
            connection.ScreenPacketReceived -= OnScreenPacketReceived;
        };

        // Keyboard shortcuts
        KeyDown += Window_KeyDown;
        
        // Pencere yüklendiğinde fit-to-screen yap
        // Pencere yüklendiğinde fit-to-screen yap
        Loaded += Window_Loaded;
    }
    
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() => FitToScreen()), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void OnScreenPacketReceived(object? sender, (ScreenPacket Packet, byte[] ImageBytes) data)
    {
        Dispatcher.Invoke(() =>
        {
            if (data.ImageBytes.Length > 0)
            {
                var screenReceiver = new ScreenReceiver();
                var bitmapImage = screenReceiver.ConvertBytesToBitmapImage(data.ImageBytes);
                if (bitmapImage != null)
                {
                    _viewModel.RemoteImage = bitmapImage;
                    UpdateImageScale();
                }
            }
        });
    }

    private void ImageBorder_MouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(RemoteImageControl);
        var (clientX, clientY) = ConvertViewToClientCoordinates(position);
        
        // Mouse pozisyonunu güncelle (status bar için)
        _viewModel.MousePosition = $"({clientX}, {clientY})";
        
        if (_viewModel.IsControlling)
        {
            _ = _viewModel.SendMouseMove(clientX, clientY);
            _lastMousePosition = position;
        }
    }

    private async void ImageBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsControlling)
        {
            _isMouseDown = true;
            var position = e.GetPosition(RemoteImageControl);
            var (clientX, clientY) = ConvertViewToClientCoordinates(position);
            
            RCS.Shared.Models.MouseButton button = e.ChangedButton switch
            {
                WpfMouseButton.Left => RCS.Shared.Models.MouseButton.Left,
                WpfMouseButton.Right => RCS.Shared.Models.MouseButton.Right,
                WpfMouseButton.Middle => RCS.Shared.Models.MouseButton.Middle,
                _ => RCS.Shared.Models.MouseButton.Left
            };
            
            await _viewModel.SendMouseDown(clientX, clientY, button);
        }
    }

    private async void ImageBorder_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsControlling && _isMouseDown)
        {
            _isMouseDown = false;
            var position = e.GetPosition(RemoteImageControl);
            var (clientX, clientY) = ConvertViewToClientCoordinates(position);
            
            RCS.Shared.Models.MouseButton button = e.ChangedButton switch
            {
                WpfMouseButton.Left => RCS.Shared.Models.MouseButton.Left,
                WpfMouseButton.Right => RCS.Shared.Models.MouseButton.Right,
                WpfMouseButton.Middle => RCS.Shared.Models.MouseButton.Middle,
                _ => RCS.Shared.Models.MouseButton.Left
            };
            
            await _viewModel.SendMouseUp(clientX, clientY, button);
        }
    }

    private void ImageBorder_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Ctrl + Scroll = Zoom
            e.Handled = true;
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            ZoomAtPosition(e.GetPosition(RemoteImageControl), zoomFactor);
        }
        else if (_viewModel.IsControlling)
        {
            int delta = e.Delta > 0 ? 120 : -120;
            _ = _viewModel.SendMouseWheel(delta);
        }
    }

    private void ImageBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_viewModel.IsControlling)
        {
            // Mouse pencereden çıktığında kontrolü bırak
            // (opsiyonel - isteğe bağlı)
        }
    }

    private void RemoteImageControl_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_viewModel.IsControlling)
        {
            // Mouse kontrolü aktifken görünürlük
            RemoteImageControl.Cursor = Cursors.Cross;
        }
        else
        {
            RemoteImageControl.Cursor = Cursors.Arrow;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Fullscreen toggle (F11)
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

        // Zoom shortcuts
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.Add || e.Key == Key.OemPlus)
            {
                ZoomAtPosition(new Point(ActualWidth / 2, ActualHeight / 2), 1.2);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                ZoomAtPosition(new Point(ActualWidth / 2, ActualHeight / 2), 0.8);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.D0)
            {
                FitToScreen();
                e.Handled = true;
                return;
            }
        }

        if (_viewModel.IsControlling)
        {
            int keyCode = KeyInterop.VirtualKeyFromKey(e.Key);
            _ = _viewModel.SendKeyPress(keyCode);
        }
    }

    private void FitToScreen_Click(object sender, RoutedEventArgs e)
    {
        FitToScreen();
    }

    private void ActualSize_Click(object sender, RoutedEventArgs e)
    {
        _currentZoom = 1.0;
        _zoomTransform.ScaleX = _currentZoom;
        _zoomTransform.ScaleY = _currentZoom;
        UpdateImageScale();
        var scrollViewer = FindScrollViewer();
        scrollViewer?.ScrollToHorizontalOffset(0);
        scrollViewer?.ScrollToVerticalOffset(0);
    }

    private void ToggleFullscreen_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullscreen();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomAtPosition(new Point(ActualWidth / 2, ActualHeight / 2), 1.2);
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomAtPosition(new Point(ActualWidth / 2, ActualHeight / 2), 0.8);
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            // Fullscreen'e geç
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _isFullscreen = true;
        }
        else
        {
            // Normal moda dön
            WindowStyle = _previousWindowStyle;
            WindowState = _previousWindowState;
            _isFullscreen = false;
        }
    }

    private void FitToScreen()
    {
        if (_viewModel.RemoteImage == null) return;

        double imageWidth = _viewModel.RemoteImage.PixelWidth;
        double imageHeight = _viewModel.RemoteImage.PixelHeight;
        
        var scrollViewer = FindScrollViewer();
        if (scrollViewer == null) return;
        
        double containerWidth = scrollViewer.ActualWidth - 20; // Scrollbar için margin
        double containerHeight = scrollViewer.ActualHeight - 20;
        
        if (containerWidth <= 0 || containerHeight <= 0) return;

        double scaleX = containerWidth / imageWidth;
        double scaleY = containerHeight / imageHeight;
        _currentZoom = Math.Min(scaleX, scaleY);
        
        _zoomTransform.ScaleX = _currentZoom;
        _zoomTransform.ScaleY = _currentZoom;
        UpdateImageScale();
        
        // Ortaya hizala
        scrollViewer.ScrollToHorizontalOffset(0);
        scrollViewer.ScrollToVerticalOffset(0);
    }

    private System.Windows.Controls.ScrollViewer? FindScrollViewer()
    {
        return FindVisualChild<System.Windows.Controls.ScrollViewer>(this);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    private void ZoomAtPosition(Point position, double zoomFactor)
    {
        if (_viewModel.RemoteImage == null) return;

        _currentZoom = Math.Max(0.25, Math.Min(4.0, _currentZoom * zoomFactor));
        
        _zoomTransform.ScaleX = _currentZoom;
        _zoomTransform.ScaleY = _currentZoom;
        UpdateImageScale();
    }

    private void UpdateImageScale()
    {
        _viewModel.ImageScale = (int)(_currentZoom * 100);
    }

    /// <summary>
    /// View koordinatlarını client ekran koordinatlarına dönüştürür
    /// </summary>
    private (int x, int y) ConvertViewToClientCoordinates(Point viewPoint)
    {
        if (RemoteImageControl.Source is BitmapImage bitmapImage)
        {
            var imageWidth = bitmapImage.PixelWidth;
            var imageHeight = bitmapImage.PixelHeight;
            
            var renderWidth = RemoteImageControl.ActualWidth;
            var renderHeight = RemoteImageControl.ActualHeight;
            
            // Stretch="Uniform" olduğu için scale factor'ü hesapla
            var scaleX = imageWidth / renderWidth;
            var scaleY = imageHeight / renderHeight;
            var scale = Math.Min(scaleX, scaleY);
            
            // Render area'daki offset'i hesapla (centered olduğu için)
            var offsetX = (renderWidth - imageWidth / scale) / 2;
            var offsetY = (renderHeight - imageHeight / scale) / 2;
            
            // View koordinatını image koordinatına dönüştür
            var clientX = (int)((viewPoint.X - offsetX) * scale);
            var clientY = (int)((viewPoint.Y - offsetY) * scale);
            
            // Sınırları kontrol et
            clientX = Math.Max(0, Math.Min(clientX, imageWidth - 1));
            clientY = Math.Max(0, Math.Min(clientY, imageHeight - 1));
            
            return (clientX, clientY);
        }
        
        // Fallback
        return ((int)viewPoint.X, (int)viewPoint.Y);
    }
}
