using System.Net;
using System.Net.Sockets;
using System.Text;
using RCS.Shared.Models;
using RCS.Shared.Protocol;
using RCS.Shared.Utils;

namespace RCS.ClientService.Services;

/// <summary>
/// TCP bağlantı yöneticisi - Agent modunda Viewer'a bağlanır veya Listener modunda dinler
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly Logger _logger;
    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;
    private bool _isConnected = false;
    private bool _disposed = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;
    private readonly object _connectionLock = new();
    
    public event EventHandler<ControlPacket>? ControlPacketReceived;
    public event EventHandler<bool>? ConnectionStatusChanged;
    
    private readonly ScreenCapturer _screenCapturer;
    private readonly InputInjector _inputInjector;
    private readonly string _serverIp;
    private readonly int _serverPort;
    private readonly string _clientId;

    public ConnectionManager(string serverIp, int serverPort, ScreenCapturer screenCapturer, InputInjector inputInjector, Logger logger)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
        _screenCapturer = screenCapturer;
        _inputInjector = inputInjector;
        _logger = logger;
        _clientId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Viewer'a bağlanır (Client mode)
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            _logger.Info($"Attempting to connect to server at {_serverIp}:{_serverPort}");
            
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_serverIp, _serverPort);
            
            _networkStream = _tcpClient.GetStream();
            
            lock (_connectionLock)
            {
                _isConnected = true;
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            // İlk bağlantı paketini gönder
            await SendConnectionPacketAsync();
            
            // Heartbeat gönderme task'ını başlat
            _ = Task.Run(() => HeartbeatLoopAsync(_cancellationTokenSource.Token));
            
            // Paket alma task'ını başlat
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token));
            
            ConnectionStatusChanged?.Invoke(this, true);
            _logger.Info("Successfully connected to server");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to connect to server", ex);
            lock (_connectionLock)
            {
                _isConnected = false;
            }
            ConnectionStatusChanged?.Invoke(this, false);
            return false;
        }
    }

    /// <summary>
    /// Bağlantıyı kapatır
    /// </summary>
    public void Disconnect()
    {
        lock (_connectionLock)
        {
            if (!_isConnected)
                return;
            _isConnected = false;
        }
        
        _cancellationTokenSource?.Cancel();
        
        try
        {
            _networkStream?.Close();
            _tcpClient?.Close();
        }
        catch (Exception ex)
        {
            _logger.Error("Error during disconnect", ex);
        }
        finally
        {
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
            ConnectionStatusChanged?.Invoke(this, false);
            _logger.Info("Disconnected from server");
        }
    }

    /// <summary>
    /// İlk bağlantı paketini gönderir (ClientInfo)
    /// </summary>
    private async Task SendConnectionPacketAsync()
    {
        if (_networkStream == null || !_isConnected)
            return;

        try
        {
            var clientInfo = new ClientInfo
            {
                ClientId = _clientId,
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                ScreenResolutions = _screenCapturer.GetAllScreenResolutions(),
                IpAddress = GetLocalIpAddress(),
                Version = "1.0.0"
            };

            var connectionPacket = new ConnectionPacket
            {
                ClientInfo = clientInfo
            };

            var packetBytes = PacketSerializer.Serialize(connectionPacket);
            var lengthPrefixed = PacketSerializer.CreateLengthPrefixedPacket(packetBytes);
            
            await _networkStream.WriteAsync(lengthPrefixed, 0, lengthPrefixed.Length);
            _logger.Debug("Connection packet sent");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to send connection packet", ex);
        }
    }

    /// <summary>
    /// ScreenPacket gönderir
    /// </summary>
    public async Task SendScreenPacketAsync(ScreenPacket packet, byte[] imageBytes)
    {
        if (_networkStream == null || !_isConnected)
            return;

        try
        {
            var packetBytes = PacketSerializer.SerializeScreenPacket(packet, imageBytes);
            await _networkStream.WriteAsync(packetBytes, 0, packetBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to send screen packet", ex);
            // Bağlantı kopmuş olabilir
            Disconnect();
        }
    }

    /// <summary>
    /// Paket alma döngüsü
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_networkStream == null)
            return;

        var buffer = new byte[4096];
        
        while (!cancellationToken.IsCancellationRequested)
        {
            bool isConnected;
            lock (_connectionLock)
            {
                isConnected = _isConnected;
            }
            
            if (!isConnected)
                break;
            
            try
            {
                // Length okuma (4 byte)
                var lengthBytes = new byte[4];
                int bytesRead = await ReadExactlyAsync(_networkStream, lengthBytes, 0, 4, cancellationToken);
                
                if (bytesRead != 4)
                {
                    _logger.Warning("Failed to read packet length, disconnecting");
                    break;
                }

                var (payloadLength, _) = PacketSerializer.ParsePacketLength(lengthBytes);
                
                if (payloadLength <= 0 || payloadLength > 10_000_000) // 10MB limit
                {
                    _logger.Warning($"Invalid packet length: {payloadLength}");
                    continue;
                }

                // Payload okuma
                var payload = new byte[payloadLength];
                bytesRead = await ReadExactlyAsync(_networkStream, payload, 0, payloadLength, cancellationToken);
                
                if (bytesRead != payloadLength)
                {
                    _logger.Warning($"Failed to read full payload. Expected: {payloadLength}, Read: {bytesRead}");
                    break;
                }

                // Paketi parse et
                var controlPacket = PacketSerializer.Deserialize<ControlPacket>(payload);
                
                if (controlPacket != null && controlPacket.PacketType == "CONTROL")
                {
                    ControlPacketReceived?.Invoke(this, controlPacket);
                    // Paketi işle
                    _inputInjector.ProcessControlPacket(controlPacket);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("Error in receive loop", ex);
                break; // Hata durumunda döngüden çık
            }
        }
        
        // Bağlantı koptu, disconnect et
        Disconnect();
    }

    /// <summary>
    /// Belirtilen byte sayısını tam olarak okur
    /// </summary>
    private static async Task<int> ReadExactlyAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalBytesRead = 0;
        
        while (totalBytesRead < count)
        {
            int bytesRead = await stream.ReadAsync(buffer, offset + totalBytesRead, count - totalBytesRead, cancellationToken);
            
            if (bytesRead == 0)
                return totalBytesRead; // Stream kapandı
            
            totalBytesRead += bytesRead;
        }
        
        return totalBytesRead;
    }

    /// <summary>
    /// Heartbeat döngüsü (her 5 saniyede bir)
    /// </summary>
    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, cancellationToken); // 5 saniye bekle
                
                bool isConnected;
                lock (_connectionLock)
                {
                    isConnected = _isConnected;
                }
                
                if (!isConnected || _networkStream == null)
                    break;

                var heartbeat = new HeartbeatPacket
                {
                    ClientId = _clientId
                };

                var packetBytes = PacketSerializer.Serialize(heartbeat);
                var lengthPrefixed = PacketSerializer.CreateLengthPrefixedPacket(packetBytes);
                
                await _networkStream.WriteAsync(lengthPrefixed, 0, lengthPrefixed.Length, cancellationToken);
                _logger.Debug("Heartbeat sent");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("Error sending heartbeat", ex);
                Disconnect();
                break;
            }
        }
    }

    /// <summary>
    /// Yerel IP adresini alır
    /// </summary>
    private static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
            // Hata durumunda localhost döndür
        }
        
        return "127.0.0.1";
    }

    public bool IsConnected
    {
        get
        {
            lock (_connectionLock)
            {
                return _isConnected && (_tcpClient?.Connected ?? false);
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _receiveTask?.Wait(TimeSpan.FromSeconds(2));
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}

