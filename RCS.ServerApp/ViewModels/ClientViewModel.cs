using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using RCS.Shared.Models;

namespace RCS.ServerApp.ViewModels;

/// <summary>
/// Client için ViewModel
/// </summary>
public class ClientViewModel : INotifyPropertyChanged
{
    private ClientInfo _clientInfo;
    private BitmapImage? _thumbnailImage;
    private bool _isOnline;
    private DateTime _lastSeen;

    public ClientViewModel(ClientInfo clientInfo)
    {
        _clientInfo = clientInfo;
        _isOnline = true;
        _lastSeen = DateTime.UtcNow;
    }

    public ClientInfo ClientInfo
    {
        get => _clientInfo;
        set
        {
            _clientInfo = value;
            OnPropertyChanged();
        }
    }

    public string ClientId => _clientInfo.ClientId;
    public string MachineName => _clientInfo.MachineName;
    public string OSVersion => _clientInfo.OSVersion;
    public string IpAddress => _clientInfo.IpAddress;

    public BitmapImage? ThumbnailImage
    {
        get => _thumbnailImage;
        set
        {
            // Eski thumbnail'i temizle (memory leak önleme)
            var oldThumbnail = _thumbnailImage;
            _thumbnailImage = value;
            OnPropertyChanged();
            
            // Eski image'i dispose et (UI thread'de değil, burada dispose edilemez ama null yapılabilir)
            // BitmapImage Freeze() edilmiş olduğu için dispose edilemez, GC tarafından temizlenir
        }
    }

    public bool IsOnline
    {
        get => _isOnline;
        set
        {
            _isOnline = value;
            OnPropertyChanged();
        }
    }

    public DateTime LastSeen
    {
        get => _lastSeen;
        set
        {
            _lastSeen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LastSeenString));
        }
    }

    public string LastSeenString => LastSeen.ToString("HH:mm:ss");

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

