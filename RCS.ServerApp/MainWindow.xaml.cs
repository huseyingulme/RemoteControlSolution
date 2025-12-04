using System.Windows;
using System.Windows.Input;
using RCS.ServerApp.Config;
using RCS.ServerApp.ViewModels;
using RCS.ServerApp.Views;

namespace RCS.ServerApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _isFullscreen = false;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode _previousResizeMode;

    public MainWindow()
    {
        InitializeComponent();
        
        // ViewModel'deki fullscreen değişikliklerini dinle
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsFullscreen))
                {
                    ToggleFullscreen();
                }
            };
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // F11 ile tam ekran
        if (e.Key == Key.F11)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.IsFullscreen = !viewModel.IsFullscreen;
            }
            e.Handled = true;
        }
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            // Fullscreen'e geç
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;
            
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullscreen = true;
        }
        else
        {
            // Normal moda dön
            WindowStyle = _previousWindowStyle;
            ResizeMode = _previousResizeMode;
            WindowState = _previousWindowState;
            _isFullscreen = false;
        }
    }

    private void OpenLogViewer_Click(object sender, RoutedEventArgs e)
    {
        var logWindow = new LogViewerWindow();
        logWindow.Show();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            var config = new ServerConfig
            {
                Port = viewModel.Port,
                AutoStart = false, // Load from config
                HeartbeatTimeoutSeconds = 15,
                MaxClients = 100,
                LogDirectory = "Logs"
            };

            var settingsWindow = new SettingsWindow(config);
            if (settingsWindow.ShowDialog() == true && settingsWindow.SavedConfig != null)
            {
                // Settings saved, reload if needed
                MessageBox.Show("Settings saved. Some changes may require application restart.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        // ViewModel'i temizle
        if (DataContext is MainViewModel viewModel)
        {
            // Cleanup işlemleri burada yapılabilir
        }
        
        Application.Current.Shutdown();
    }
}
