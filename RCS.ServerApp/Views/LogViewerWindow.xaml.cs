using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RCS.Shared.Utils;

namespace RCS.ServerApp.Views;

/// <summary>
/// Interaction logic for LogViewerWindow.xaml
/// </summary>
public partial class LogViewerWindow : Window
{
    private readonly string _logFilePath;
    private FileSystemWatcher? _fileWatcher;

    public LogViewerWindow()
    {
        InitializeComponent();
        _logFilePath = Path.Combine("Logs", "server.log");
        LoadLogs();
        StartWatching();
    }

    private void LoadLogs()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                var lines = File.ReadAllLines(_logFilePath);
                var filteredLines = FilterLines(lines);
                LogTextBox.Text = string.Join(Environment.NewLine, filteredLines);
                
                if (AutoScrollCheckBox.IsChecked == true)
                {
                    LogScrollViewer.ScrollToEnd();
                }
                
                StatusTextBlock.Text = $"Loaded {filteredLines.Length} log entries";
            }
            else
            {
                LogTextBox.Text = "Log file not found.";
                StatusTextBlock.Text = "Log file not found";
            }
        }
        catch (Exception ex)
        {
            LogTextBox.Text = $"Error loading logs: {ex.Message}";
            StatusTextBlock.Text = "Error loading logs";
        }
    }

    private string[] FilterLines(string[] lines)
    {
        var selectedLevel = ((ComboBoxItem)LogLevelComboBox.SelectedItem)?.Content?.ToString() ?? "All";
        
        if (selectedLevel == "All")
            return lines;

        return lines.Where(line =>
        {
            var level = selectedLevel.ToUpper();
            return line.Contains($"[{level}]", StringComparison.OrdinalIgnoreCase);
        }).ToArray();
    }

    private void StartWatching()
    {
        try
        {
            var logDir = Path.GetDirectoryName(_logFilePath);
            if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                return;

            _fileWatcher = new FileSystemWatcher(logDir, "server.log")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            
            _fileWatcher.Changed += (s, e) =>
            {
                Dispatcher.Invoke(() => LoadLogs());
            };
            
            _fileWatcher.EnableRaisingEvents = true;
        }
        catch
        {
            // Ignore watcher errors
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
        StatusTextBlock.Text = "Logs cleared";
    }

    private void LogLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadLogs();
    }

    protected override void OnClosed(EventArgs e)
    {
        _fileWatcher?.Dispose();
        base.OnClosed(e);
    }
}

