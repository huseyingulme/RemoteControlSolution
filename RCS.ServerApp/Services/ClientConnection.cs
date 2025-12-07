using System.Net.Sockets;
using System.Text;
using RCS.Shared.Models;
using RCS.Shared.Protocol;
using RCS.Shared.Utils;

namespace RCS.ServerApp.Services;

/// <summary>
/// Tek bir client bağlantısını yönetir
/// </summary>
public class ClientConnection : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _networkStream;
    private readonly Logger _logger;
    private bool _disposed = false;
    private DateTime _lastHeartbeat = DateTime.UtcNow;
    private readonly int _heartbeatTimeoutSeconds;
    private Task? _timeoutCheckTask;
    private CancellationTokenSource? _timeoutCts;
    private readonly object _heartbeatLock = new();
    
    public ClientInfo? ClientInfo { get; set; }
    
    public event EventHandler? TimeoutDetected;
    public event EventHandler<(ScreenPacket Packet, byte[] ImageBytes)>? ScreenPacketReceived;
    public event EventHandler<ControlPacket>? ControlPacketReceived;
    public event EventHandler<HeartbeatPacket>? HeartbeatReceived;

    public ClientConnection(TcpClient tcpClient, NetworkStream networkStream, Logger logger)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        _networkStream = networkStream ?? throw new ArgumentNullException(nameof(networkStream));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _heartbeatTimeoutSeconds = 15; // Default timeout
    }

    /// <summary>
    /// İlk bağlantı paketini alır
    /// </summary>
    public async Task<ConnectionPacket?> ReceiveConnectionPacketAsync(CancellationToken cancellationToken)
    {
        try
        {
            var payload = await ReadPacketAsync(cancellationToken);
            if (payload == null)
                return null;

            var packet = PacketSerializer.Deserialize<ConnectionPacket>(payload);
            return packet;
        }
        catch (Exception ex)
        {
            _logger.Error("Error receiving connection packet", ex);
            return null;
        }
    }

    /// <summary>
    /// Paket alma döngüsü
    /// </summary>
    public async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _tcpClient.Connected)
        {
            try
            {
                // Length okuma (4 byte)
                var lengthBytes = new byte[4];
                int bytesRead = await ReadExactlyAsync(_networkStream, lengthBytes, 0, 4, cancellationToken);
                
                if (bytesRead != 4)
                {
                    _logger.Debug("Failed to read packet length");
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
                    continue;
                }

                // Büyük paketler muhtemelen ScreenPacket (JSON header + binary image)
                if (payloadLength > 1000)
                {
                    try
                    {
                        // Full packet'i oluştur (length + payload)
                        var fullPacket = new byte[4 + payloadLength];
                        Array.Copy(lengthBytes, fullPacket, 4);
                        Array.Copy(payload, 0, fullPacket, 4, payloadLength);
                        
                        var (packet, imageBytes) = PacketSerializer.DeserializeScreenPacket(fullPacket);
                        ScreenPacketReceived?.Invoke(this, (packet, imageBytes));
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Error deserializing screen packet, trying normal parsing", ex);
                        // Fall through to normal parsing
                    }
                }

                // Normal paket parse (JSON only)
                var jsonString = Encoding.UTF8.GetString(payload);
                
                if (jsonString.Contains("\"packetType\":\"SCREEN\"") || jsonString.Contains("\"PacketType\":\"SCREEN\""))
                {
                    // ScreenPacket - JSON only (imageBytes yok)
                    var packet = PacketSerializer.Deserialize<ScreenPacket>(payload);
                    if (packet != null)
                    {
                        ScreenPacketReceived?.Invoke(this, (packet, Array.Empty<byte>()));
                    }
                }
                else if (jsonString.Contains("\"packetType\":\"CONTROL\"") || jsonString.Contains("\"PacketType\":\"CONTROL\""))
                {
                    var packet = PacketSerializer.Deserialize<ControlPacket>(payload);
                    if (packet != null)
                    {
                        ControlPacketReceived?.Invoke(this, packet);
                    }
                }
                else if (jsonString.Contains("\"packetType\":\"HEARTBEAT\"") || jsonString.Contains("\"PacketType\":\"HEARTBEAT\""))
                {
                    var packet = PacketSerializer.Deserialize<HeartbeatPacket>(payload);
                    if (packet != null)
                    {
                        lock (_heartbeatLock)
                        {
                            _lastHeartbeat = DateTime.UtcNow;
                        }
                        HeartbeatReceived?.Invoke(this, packet);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("Error in receive loop", ex);
                break;
            }
        }
    }

    /// <summary>
    /// Length-prefixed paketi okur
    /// </summary>
    private async Task<byte[]?> ReadPacketAsync(CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[4];
        int bytesRead = await ReadExactlyAsync(_networkStream, lengthBytes, 0, 4, cancellationToken);
        
        if (bytesRead != 4)
            return null;

        var (payloadLength, _) = PacketSerializer.ParsePacketLength(lengthBytes);
        
        if (payloadLength <= 0 || payloadLength > 10_000_000)
            return null;

        var payload = new byte[payloadLength];
        bytesRead = await ReadExactlyAsync(_networkStream, payload, 0, payloadLength, cancellationToken);
        
        if (bytesRead != payloadLength)
            return null;

        return payload;
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
    /// ControlPacket gönderir
    /// </summary>
    public async Task SendControlPacketAsync(ControlPacket packet)
    {
        if (packet == null)
            throw new ArgumentNullException(nameof(packet));
        
        if (!IsConnected || _networkStream == null)
            return;
        
        try
        {
            var packetBytes = PacketSerializer.Serialize(packet);
            var lengthPrefixed = PacketSerializer.CreateLengthPrefixedPacket(packetBytes);
            
            await _networkStream.WriteAsync(lengthPrefixed, 0, lengthPrefixed.Length);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to send control packet", ex);
            // Bağlantı kopmuş olabilir
            Disconnect();
        }
    }

    /// <summary>
    /// Bağlantıyı kapatır
    /// </summary>
    public void Disconnect()
    {
        try
        {
            _networkStream?.Close();
            _tcpClient?.Close();
        }
        catch
        {
            // Ignore
        }
    }

    public bool IsConnected => _tcpClient?.Connected ?? false;
    
    public DateTime LastHeartbeat
    {
        get
        {
            lock (_heartbeatLock)
            {
                return _lastHeartbeat;
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
            _disposed = true;
        }
    }
}

