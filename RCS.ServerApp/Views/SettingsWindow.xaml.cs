using System.IO;
using System.Text.Json;
using System.Windows;
using RCS.ServerApp.Config;

namespace RCS.ServerApp.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly string _configFilePath = "serversettings.json";
    private ServerConfig _config;

    public ServerConfig? SavedConfig { get; private set; }

    public SettingsWindow(ServerConfig currentConfig)
    {
        InitializeComponent();
        _config = currentConfig;
        LoadSettings();
    }

    private void LoadSettings()
    {
        PortTextBox.Text = _config.Port.ToString();
        AutoStartCheckBox.IsChecked = _config.AutoStart;
        MaxClientsTextBox.Text = _config.MaxClients.ToString();
        HeartbeatTimeoutTextBox.Text = _config.HeartbeatTimeoutSeconds.ToString();
        LogDirectoryTextBox.Text = _config.LogDirectory;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (int.TryParse(PortTextBox.Text, out int port) && port > 0 && port < 65536)
            {
                _config.Port = port;
            }
            else
            {
                MessageBox.Show("Invalid port number. Must be between 1 and 65535.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _config.AutoStart = AutoStartCheckBox.IsChecked ?? false;
            
            if (int.TryParse(MaxClientsTextBox.Text, out int maxClients) && maxClients > 0)
            {
                _config.MaxClients = maxClients;
            }
            
            if (int.TryParse(HeartbeatTimeoutTextBox.Text, out int timeout) && timeout > 0)
            {
                _config.HeartbeatTimeoutSeconds = timeout;
            }
            
            _config.LogDirectory = LogDirectoryTextBox.Text;

            // Save to file
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, json);

            SavedConfig = _config;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

