using RCS.ClientService.Services;
using RCS.Shared.Models;
using RCS.Shared.Protocol;
using RCS.Shared.Utils;

namespace RCS.ClientService.Services;

/// <summary>
/// Ana agent servisi - ekran yakalama ve gönderme döngüsünü yönetir
/// </summary>
public class AgentService : IDisposable
{
    private readonly ConnectionManager _connectionManager;
    private readonly ScreenCapturer _screenCapturer;
    private readonly Logger _logger;
    private CancellationTokenSource? _captureCancellationTokenSource;
    private Task? _captureTask;
    private bool _disposed = false;
    private long _sequenceNumber = 0;

    // Konfigürasyon
    private readonly int _captureIntervalMs; // Her X ms'de bir frame yakala
    private readonly int _jpegQuality;
    private readonly int? _maxWidth;
    private readonly int? _maxHeight;
    private int _reconnectAttempts = 0;
    private const int MaxReconnectDelaySeconds = 60; // Maksimum 60 saniye bekleme

    public AgentService(
        ConnectionManager connectionManager,
        ScreenCapturer screenCapturer,
        Logger logger,
        int captureIntervalMs = 100,
        int jpegQuality = 75,
        int? maxWidth = null,
        int? maxHeight = null)
    {
        _connectionManager = connectionManager;
        _screenCapturer = screenCapturer;
        _logger = logger;
        _captureIntervalMs = captureIntervalMs;
        _jpegQuality = jpegQuality;
        _maxWidth = maxWidth;
        _maxHeight = maxHeight;

        // Bağlantı durumu değişikliğini dinle
        _connectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
        _connectionManager.ControlPacketReceived += OnControlPacketReceived;
    }

    /// <summary>
    /// Servisi başlatır
    /// </summary>
    public async Task StartAsync()
    {
        _logger.Info("Starting Agent Service...");
        
        // Önce bağlan
        bool connected = await _connectionManager.ConnectAsync();
        
        if (!connected)
        {
            _logger.Error("Failed to establish connection, cannot start service");
            return;
        }

        // Ekran yakalama döngüsünü başlat
        _captureCancellationTokenSource = new CancellationTokenSource();
        _captureTask = Task.Run(() => CaptureLoopAsync(_captureCancellationTokenSource.Token));
        
        _logger.Info("Agent Service started successfully");
    }

    /// <summary>
    /// Servisi durdurur
    /// </summary>
    public void Stop()
    {
        _logger.Info("Stopping Agent Service...");
        
        _captureCancellationTokenSource?.Cancel();
        _captureTask?.Wait(TimeSpan.FromSeconds(5));
        
        _connectionManager.Disconnect();
        
        _logger.Info("Agent Service stopped");
    }

    /// <summary>
    /// Ekran yakalama döngüsü
    /// </summary>
    private async Task CaptureLoopAsync(CancellationToken cancellationToken)
    {
        _logger.Info($"Starting capture loop (interval: {_captureIntervalMs}ms)");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_connectionManager.IsConnected)
                {
                    // Exponential backoff ile yeniden bağlanmayı dene
                    int delaySeconds = Math.Min((int)Math.Pow(2, _reconnectAttempts), MaxReconnectDelaySeconds);
                    _logger.Info($"Not connected. Attempting reconnect in {delaySeconds} seconds (attempt {_reconnectAttempts + 1})");
                    
                    await Task.Delay(delaySeconds * 1000, cancellationToken);
                    
                    bool connected = await _connectionManager.ConnectAsync();
                    if (connected)
                    {
                        _reconnectAttempts = 0; // Başarılı bağlantı, counter'ı sıfırla
                        _logger.Info("Reconnected successfully");
                    }
                    else
                    {
                        _reconnectAttempts++;
                    }
                    continue;
                }

                var startTime = DateTime.UtcNow;
                
                // Ekran görüntüsünü yakala ve sıkıştır
                byte[] imageBytes = _screenCapturer.CaptureScreenAsJpeg(_jpegQuality, _maxWidth, _maxHeight);
                var resolution = _screenCapturer.GetScreenResolution();

                // ScreenPacket oluştur
                var packet = new ScreenPacket
                {
                    Sequence = Interlocked.Increment(ref _sequenceNumber),
                    Timestamp = DateTime.UtcNow,
                    Width = resolution.Width,
                    Height = resolution.Height,
                    Format = "jpeg",
                    ImageLength = imageBytes.Length
                };

                // Paketi gönder
                await _connectionManager.SendScreenPacketAsync(packet, imageBytes);
                
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.Debug($"Frame {packet.Sequence} sent ({imageBytes.Length} bytes, {elapsed:F1}ms)");

                // Bir sonraki frame için bekle
                var delay = Math.Max(0, _captureIntervalMs - (int)elapsed);
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("Error in capture loop", ex);
                await Task.Delay(1000, cancellationToken); // Hata durumunda kısa bekleme
            }
        }
        
        _logger.Info("Capture loop ended");
    }

    /// <summary>
    /// Bağlantı durumu değiştiğinde çağrılır
    /// </summary>
    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            _logger.Info("Connected to server");
        }
        else
        {
            _logger.Warning("Disconnected from server");
        }
    }

    /// <summary>
    /// Kontrol paketi alındığında çağrılır
    /// </summary>
    private void OnControlPacketReceived(object? sender, ControlPacket packet)
    {
        _logger.Debug($"Control packet received: {packet.Type} at ({packet.X}, {packet.Y})");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _captureCancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}

