using System.IO;

namespace RCS.Shared.Utils;

/// <summary>
/// Basit dosya tabanlı logger
/// </summary>
public class Logger
{
    private readonly string _logDirectory;
    private readonly string _logFileName;
    private readonly object _lockObject = new();

    public Logger(string logDirectory = "Logs", string logFileName = "app.log")
    {
        _logDirectory = logDirectory;
        _logFileName = logFileName;
        
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    private string LogFilePath => Path.Combine(_logDirectory, _logFileName);

    private void WriteLog(LogLevel level, string message, Exception? exception = null)
    {
        lock (_lockObject)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}";
            }
            
            logEntry += Environment.NewLine;
            
            File.AppendAllText(LogFilePath, logEntry);
            
            // Konsol çıktısı (opsiyonel)
            Console.WriteLine(logEntry.TrimEnd());
        }
    }

    public void Info(string message) => WriteLog(LogLevel.INFO, message);
    public void Warning(string message) => WriteLog(LogLevel.WARNING, message);
    public void Error(string message, Exception? exception = null) => WriteLog(LogLevel.ERROR, message, exception);
    public void Debug(string message) => WriteLog(LogLevel.DEBUG, message);
}

public enum LogLevel
{
    DEBUG,
    INFO,
    WARNING,
    ERROR
}

