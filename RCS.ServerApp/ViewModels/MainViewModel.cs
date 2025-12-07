using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using RCS.ServerApp.Config;
using RCS.ServerApp.Services;
using RCS.Shared.Models;
using RCS.Shared.Utils;

namespace RCS.ServerApp.ViewModels;

/// <summary>
/// Ana ViewModel - Client listesini ve listener'ı yönetir
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ListenerService _listenerService;
    private readonly ScreenReceiver _screenReceiver;
    private readonly ControlSender _controlSender;
    private readonly Logger _logger;
    private readonly ServerConfig _config;
    private bool _isListening;
    private string _statusMessage = "Ready";
    private int _port = 9999;
    private string _localIpAddress = "Loading...";
    private string _searchText = "";
    private ObservableCollection<ClientViewModel> _filteredClients;
    private bool _isFullscreen = false;

    private const string ConfigFilePath = "serversettings.json";

    public MainViewModel()
    {
        Clients = new ObservableCollection<ClientViewModel>();
        _filteredClients = new ObservableCollection<ClientViewModel>();
        _config = LoadConfig();
        _port = _config.Port;
        _logger = new Logger(_config.LogDirectory, "server.log");
        _listenerService = new ListenerService(_logger);
        _screenReceiver = new ScreenReceiver();
        _controlSender = new ControlSender(_listenerService);
        
        OnPropertyChanged(nameof(Port));
        
        // IP adresini al
        _ = Task.Run(LoadLocalIpAddress);

        // Event handler'ları bağla
        _listenerService.ClientConnected += OnClientConnected;
        _listenerService.ClientDisconnected += OnClientDisconnected;
        
        // Clients collection değiştiğinde filtrele
        Clients.CollectionChanged += (s, e) => FilterClients();

        // Command'ları başlat
        StartListeningCommand = new RelayCommand(async () => await StartListeningAsync(), () => !_isListening && _port > 0 && _port < 65536);
        StopListeningCommand = new RelayCommand(() => StopListening(), () => _isListening);
        OpenRemoteViewCommand = new RelayCommand<string>(OpenRemoteView, (clientId) => !string.IsNullOrEmpty(clientId));
        DisconnectClientCommand = new RelayCommand<string>(DisconnectClient, (clientId) => !string.IsNullOrEmpty(clientId));
        CopyIpAddressCommand = new RelayCommand(CopyIpAddress);
        ToggleFullscreenCommand = new RelayCommand(ToggleFullscreen);
        
        // Auto-start if configured
        if (_config.AutoStart)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // UI hazır olsun
                await StartListeningAsync();
            });
        }
    }

    public ObservableCollection<ClientViewModel> Clients { get; }
    
    public ObservableCollection<ClientViewModel> FilteredClients
    {
        get => _filteredClients;
        set
        {
            _filteredClients = value;
            OnPropertyChanged();
        }
    }

    public bool IsListening
    {
        get => _isListening;
        set
        {
            _isListening = value;
            OnPropertyChanged();
            ((RelayCommand)StartListeningCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopListeningCommand).RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            if (_isListening) return; // Dinlerken değiştirilemez
            _port = value;
            OnPropertyChanged();
        }
    }

    public string LocalIpAddress
    {
        get => _localIpAddress;
        set
        {
            _localIpAddress = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConnectionInfo));
        }
    }
    
    public string ConnectionInfo => $"{LocalIpAddress}:{Port}";
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            FilterClients();
        }
    }
    
    public bool IsFullscreen
    {
        get => _isFullscreen;
        set
        {
            _isFullscreen = value;
            OnPropertyChanged();
        }
    }

    public ICommand StartListeningCommand { get; }
    public ICommand StopListeningCommand { get; }
    public ICommand OpenRemoteViewCommand { get; }
    public ICommand DisconnectClientCommand { get; }
    public ICommand CopyIpAddressCommand { get; }
    public ICommand ToggleFullscreenCommand { get; }

    private async Task StartListeningAsync()
    {
        try
        {
            await _listenerService.StartAsync(_port);
            IsListening = true;
            StatusMessage = $"Listening on port {_port}";
            _logger.Info($"Server started listening on port {_port}");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start listening", ex);
            StatusMessage = $"Error: {ex.Message}";
            IsListening = false;
        }
    }

    private void StopListening()
    {
        _listenerService.Stop();
        IsListening = false;
        StatusMessage = "Stopped";
        _logger.Info("Server stopped listening");
    }

    private void OnClientConnected(object? sender, ClientConnection connection)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var clientInfo = connection.ClientInfo;
            if (clientInfo == null)
                return;

            // Yeni client ekle
            var clientViewModel = new ClientViewModel(clientInfo);
            Clients.Add(clientViewModel);
            FilterClients();

            // Screen packet event'ini bağla
            connection.ScreenPacketReceived += (s, data) => OnScreenPacketReceived(clientInfo.ClientId, data.Packet, data.ImageBytes, connection);
            connection.HeartbeatReceived += (s, packet) => OnHeartbeatReceived(clientInfo.ClientId);

            StatusMessage = $"Client connected: {clientInfo.MachineName}";
            _logger.Info($"Client connected: {clientInfo.MachineName} ({clientInfo.ClientId})");
        });
    }

    private void OnClientDisconnected(object? sender, string clientId)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var client = Clients.FirstOrDefault(c => c.ClientId == clientId);
            if (client != null)
            {
                client.IsOnline = false;
                client.LastSeen = DateTime.UtcNow;
                // Optionally remove: Clients.Remove(client);
            }

            StatusMessage = $"Client disconnected: {clientId}";
            _logger.Info($"Client disconnected: {clientId}");
        });
    }

    private void OnScreenPacketReceived(string clientId, ScreenPacket packet, byte[] imageBytes, ClientConnection connection)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                var client = Clients.FirstOrDefault(c => c.ClientId == clientId);
                if (client == null)
                    return;

                client.LastSeen = DateTime.UtcNow;
                
                // Thumbnail oluştur (isteğe bağlı) - sadece belirli aralıklarla güncelle (performans için)
                // Her 2 saniyede bir thumbnail güncelle (performans optimizasyonu)
                var timeSinceLastUpdate = DateTime.UtcNow - client.LastSeen;
                if (imageBytes.Length > 0 && timeSinceLastUpdate.TotalSeconds < 2)
                {
                    try
                    {
                        var thumbnail = _screenReceiver.ConvertBytesToBitmapImage(imageBytes);
                        if (thumbnail != null)
                        {
                            client.ThumbnailImage = thumbnail;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Error creating thumbnail: {ex.Message}");
                        // Ignore thumbnail errors - non-critical
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error processing screen packet", ex);
            }
        });
    }

    private void OnHeartbeatReceived(string clientId)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var client = Clients.FirstOrDefault(c => c.ClientId == clientId);
            if (client != null)
            {
                client.LastSeen = DateTime.UtcNow;
                client.IsOnline = true;
            }
        });
    }

    private void OpenRemoteView(string? clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            return;

        var client = Clients.FirstOrDefault(c => c.ClientId == clientId);
        if (client == null)
            return;

        var connection = _listenerService.GetConnection(clientId);
        if (connection == null)
        {
            StatusMessage = $"Client {client.MachineName} is not connected";
            return;
        }

        // Remote view penceresini aç
        var remoteViewWindow = new Views.RemoteViewWindow(client, connection, _screenReceiver, _controlSender);
        remoteViewWindow.Show();
    }

    private void DisconnectClient(string? clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            return;

        var connection = _listenerService.GetConnection(clientId);
        if (connection != null)
        {
            connection.Disconnect();
            StatusMessage = $"Disconnected client: {clientId}";
        }
    }

    private ServerConfig LoadConfig()
    {
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<ServerConfig>(json);
                if (config != null)
                {
                    // Validate config
                    if (config.Port < 1 || config.Port > 65535)
                    {
                        _logger?.Warning($"Invalid port in config: {config.Port}, using default 9999");
                        config.Port = 9999;
                    }
                    if (config.HeartbeatTimeoutSeconds < 5)
                    {
                        config.HeartbeatTimeoutSeconds = 15;
                    }
                    if (config.MaxClients < 1)
                    {
                        config.MaxClients = 100;
                    }
                    return config;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to load server config: {ex.Message}", ex);
            }
        }

        var defaultConfig = new ServerConfig();
        SaveConfig(defaultConfig);
        return defaultConfig;
    }

    private void SaveConfig(ServerConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void LoadLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        LocalIpAddress = ip.ToString();
                    });
                    return;
                }
            }
            
            // Fallback: localhost
            App.Current.Dispatcher.Invoke(() =>
            {
                LocalIpAddress = "127.0.0.1";
            });
        }
        catch
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                LocalIpAddress = "127.0.0.1";
            });
        }
    }
    
    private void CopyIpAddress()
    {
        try
        {
            System.Windows.Clipboard.SetText(ConnectionInfo);
            StatusMessage = $"Copied: {ConnectionInfo}";
        }
        catch
        {
            StatusMessage = "Failed to copy IP address";
        }
    }
    
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }
    
    private void FilterClients()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            FilteredClients.Clear();
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var client in Clients)
                {
                    FilteredClients.Add(client);
                }
            }
            else
            {
                var searchLower = SearchText.ToLower();
                foreach (var client in Clients)
                {
                    if (client.MachineName.ToLower().Contains(searchLower) ||
                        client.IpAddress.ToLower().Contains(searchLower) ||
                        client.ClientId.ToLower().Contains(searchLower))
                    {
                        FilteredClients.Add(client);
                    }
                }
            }
        });
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Basit RelayCommand implementasyonu
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

