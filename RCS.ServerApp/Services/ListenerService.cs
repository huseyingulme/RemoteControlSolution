using System.Net;
using System.Net.Sockets;
using RCS.Shared.Models;
using RCS.Shared.Protocol;
using RCS.Shared.Utils;

namespace RCS.ServerApp.Services;

/// <summary>
/// TCP listener servisi - Client bağlantılarını kabul eder
/// </summary>
public class ListenerService : IDisposable
{
    private readonly Logger _logger;
    private TcpListener? _tcpListener;
    private bool _isListening = false;
    private bool _disposed = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _acceptTask;
    
    private readonly Dictionary<string, ClientConnection> _connections = new();
    private readonly object _connectionsLock = new();

    public event EventHandler<ClientConnection>? ClientConnected;
    public event EventHandler<string>? ClientDisconnected;

    public ListenerService(Logger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Belirtilen portta dinlemeye başlar
    /// </summary>
    public async Task StartAsync(int port)
    {
        if (_isListening)
        {
            _logger.Warning("Listener is already running");
            return;
        }

        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();
            _isListening = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _logger.Info($"TCP Listener started on port {port}");

            _acceptTask = Task.Run(() => AcceptLoopAsync(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to start listener on port {port}", ex);
            throw;
        }
    }

    /// <summary>
    /// Dinlemeyi durdurur
    /// </summary>
    public void Stop()
    {
        if (!_isListening)
            return;

        _isListening = false;
        _cancellationTokenSource?.Cancel();

        lock (_connectionsLock)
        {
            foreach (var connection in _connections.Values.ToList())
            {
                connection.Disconnect();
            }
            _connections.Clear();
        }

        _tcpListener?.Stop();
        _logger.Info("TCP Listener stopped");
    }

    /// <summary>
    /// Yeni bağlantıları kabul eden döngü
    /// </summary>
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        if (_tcpListener == null)
            return;

        while (!cancellationToken.IsCancellationRequested && _isListening)
        {
            try
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                _logger.Info($"New client connected from {tcpClient.Client.RemoteEndPoint}");
                
                // Her bağlantı için ayrı task
                _ = Task.Run(() => HandleClientAsync(tcpClient, cancellationToken), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Listener kapatıldı
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("Error accepting client connection", ex);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Client bağlantısını yönetir
    /// </summary>
    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        ClientConnection? connection = null;
        
        try
        {
            var stream = tcpClient.GetStream();
            connection = new ClientConnection(tcpClient, stream, _logger);
            
            // İlk bağlantı paketini bekle (ClientInfo)
            var connectionPacket = await connection.ReceiveConnectionPacketAsync(cancellationToken);
            
            if (connectionPacket == null || connectionPacket.ClientInfo == null)
            {
                _logger.Warning("Failed to receive connection packet");
                tcpClient.Close();
                return;
            }

            var clientInfo = connectionPacket.ClientInfo;
            connection.ClientInfo = clientInfo;
            
            _logger.Info($"Client registered: {clientInfo.MachineName} ({clientInfo.ClientId})");

            // Bağlantıyı kaydet
            lock (_connectionsLock)
            {
                _connections[clientInfo.ClientId] = connection;
            }

            ClientConnected?.Invoke(this, connection);

            // Bağlantı kapanana kadar bekle
            await connection.ReceiveLoopAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal kapanma
        }
        catch (Exception ex)
        {
            _logger.Error("Error handling client connection", ex);
        }
        finally
        {
            if (connection != null)
            {
                var clientId = connection.ClientInfo?.ClientId;
                
                lock (_connectionsLock)
                {
                    if (clientId != null && _connections.ContainsKey(clientId))
                    {
                        _connections.Remove(clientId);
                    }
                }

                connection.Disconnect();
                
                if (clientId != null)
                {
                    ClientDisconnected?.Invoke(this, clientId);
                    _logger.Info($"Client disconnected: {clientId}");
                }
            }
            else
            {
                tcpClient?.Close();
            }
        }
    }

    /// <summary>
    /// Bağlı client'ı getirir
    /// </summary>
    public ClientConnection? GetConnection(string clientId)
    {
        lock (_connectionsLock)
        {
            _connections.TryGetValue(clientId, out var connection);
            return connection;
        }
    }

    /// <summary>
    /// Tüm bağlı client'ları getirir
    /// </summary>
    public IEnumerable<ClientConnection> GetAllConnections()
    {
        lock (_connectionsLock)
        {
            return _connections.Values.ToList();
        }
    }

    public bool IsListening => _isListening;

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _acceptTask?.Wait(TimeSpan.FromSeconds(2));
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}

