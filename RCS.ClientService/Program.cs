using RCS.ClientService.Config;
using RCS.ClientService.Services;
using RCS.Shared.Utils;
using System.Text.Json;

namespace RCS.ClientService;

class Program
{
    private static AgentService? _agentService;
    private static Logger? _logger;
    private static readonly string ConfigFilePath = "agentsettings.json";

    static async Task Main(string[] args)
    {
        _logger = new Logger("Logs", "agent.log");
        _logger.Info("=== RCS Client Service Starting ===");

        try
        {
            // Konfigürasyonu yükle
            var config = LoadConfig();
            _logger.Info($"Configuration loaded: Server={config.ServerIp}:{config.ServerPort}");

            // Servisleri oluştur
            var screenCapturer = new ScreenCapturer();
            var inputInjector = new InputInjector();
            var connectionManager = new ConnectionManager(
                config.ServerIp,
                config.ServerPort,
                screenCapturer,
                inputInjector,
                _logger
            );

            _agentService = new AgentService(
                connectionManager,
                screenCapturer,
                _logger,
                config.CaptureIntervalMs,
                config.JpegQuality,
                config.MaxWidth,
                config.MaxHeight
            );

            // Servisi başlat
            await _agentService.StartAsync();

            // Konsol uygulaması olarak çalışıyorsa, kapanmayı bekle
            _logger.Info("Agent Service is running. Press Ctrl+C to stop...");
            
            // Graceful shutdown için Ctrl+C yakala
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _logger.Info("Shutdown signal received...");
                _agentService?.Stop();
                Environment.Exit(0);
            };

            // Ana thread'i canlı tut
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            _logger?.Error("Fatal error in main", ex);
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Konfigürasyon dosyasını yükler, yoksa varsayılan oluşturur
    /// </summary>
    private static AgentConfig LoadConfig()
    {
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AgentConfig>(json);
                
                if (config != null)
                    return config;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to load config file: {ex.Message}. Using defaults.");
            }
        }

        // Varsayılan konfigürasyon oluştur ve kaydet
        var defaultConfig = new AgentConfig();
        SaveConfig(defaultConfig);
        
        return defaultConfig;
    }

    /// <summary>
    /// Konfigürasyonu dosyaya kaydeder
    /// </summary>
    private static void SaveConfig(AgentConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
            _logger?.Info($"Default config saved to {ConfigFilePath}");
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to save config file: {ex.Message}");
        }
    }
}
