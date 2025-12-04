using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using RCS.ServerApp.Services;
using RCS.Shared.Models;
using SharedMouseButton = RCS.Shared.Models.MouseButton;

namespace RCS.ServerApp.ViewModels;

/// <summary>
/// Remote View penceresi için ViewModel
/// </summary>
public class RemoteViewViewModel : INotifyPropertyChanged
{
    private readonly ClientConnection _connection;
    private readonly ScreenReceiver _screenReceiver;
    private readonly ControlSender _controlSender;
    private BitmapImage? _remoteImage;
    private bool _isControlling;
    private string _title;
    private string _mousePosition = "(0, 0)";
    private int _imageScale = 100;

    public RemoteViewViewModel(ClientViewModel client, ClientConnection connection, ScreenReceiver screenReceiver, ControlSender controlSender)
    {
        Client = client;
        _connection = connection;
        _screenReceiver = screenReceiver;
        _controlSender = controlSender;
        _title = $"Remote View - {client.MachineName}";
        _isControlling = false;

        // Screen packet event'ini bağla - event artık tuple döndürüyor, bu ViewModel'de işleniyor

        // Command'lar
        StartControlCommand = new RelayCommand(() => IsControlling = true, () => !_isControlling);
        StopControlCommand = new RelayCommand(() => IsControlling = false, () => _isControlling);
    }

    public ClientViewModel Client { get; }

    public BitmapImage? RemoteImage
    {
        get => _remoteImage;
        set
        {
            _remoteImage = value;
            OnPropertyChanged();
        }
    }

    public bool IsControlling
    {
        get => _isControlling;
        set
        {
            _isControlling = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ControlStatusText));
        }
    }

    public string ControlStatusText => IsControlling ? "Controlling" : "View Only";

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
        }
    }

    public ICommand StartControlCommand { get; }
    public ICommand StopControlCommand { get; }

    public string MousePosition
    {
        get => _mousePosition;
        set
        {
            _mousePosition = value;
            OnPropertyChanged();
        }
    }

    public int ImageScale
    {
        get => _imageScale;
        set
        {
            _imageScale = value;
            OnPropertyChanged();
        }
    }


    public async Task SendMouseMove(int x, int y)
    {
        if (IsControlling)
        {
            await _controlSender.SendMouseMoveAsync(Client.ClientId, x, y);
        }
    }

    public async Task SendMouseClick(int x, int y, SharedMouseButton button)
    {
        if (IsControlling)
        {
            await _controlSender.SendMouseClickAsync(Client.ClientId, x, y, button);
        }
    }

    public async Task SendMouseDown(int x, int y, SharedMouseButton button)
    {
        if (IsControlling)
        {
            await _controlSender.SendMouseDownAsync(Client.ClientId, x, y, button);
        }
    }

    public async Task SendMouseUp(int x, int y, SharedMouseButton button)
    {
        if (IsControlling)
        {
            await _controlSender.SendMouseUpAsync(Client.ClientId, x, y, button);
        }
    }

    public async Task SendMouseWheel(int delta)
    {
        if (IsControlling)
        {
            await _controlSender.SendMouseWheelAsync(Client.ClientId, delta);
        }
    }

    public async Task SendKeyPress(int keyCode)
    {
        if (IsControlling)
        {
            await _controlSender.SendKeyPressAsync(Client.ClientId, keyCode);
        }
    }

    public async Task SendText(string text)
    {
        if (IsControlling)
        {
            await _controlSender.SendTextAsync(Client.ClientId, text);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

