namespace Keyvora.Desktop.UI.Views;

using System.Windows;
using System.Windows.Media;

public partial class PluginsDialog : Window
{
    private readonly ViewModels.PluginsViewModel _vm;

    public PluginsDialog(ViewModels.PluginsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        UpdateSpotifyUI();
        StreamlabsPasswordBox.Password = vm.StreamlabsPassword;
        UpdateStreamlabsUI();

        vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_vm.IsSpotifyAuthorized):
            case nameof(_vm.SpotifyStatus):
                UpdateSpotifyUI();
                break;
            case nameof(_vm.IsStreamlabsConnected):
            case nameof(_vm.StreamlabsConnectionStatus):
                UpdateStreamlabsUI();
                break;
            case nameof(_vm.DiscordStatus):
                UpdateDiscordUI();
                break;
        }
    }

    private void UpdateSpotifyUI()
    {
        if (_vm.IsSpotifyAuthorized)
        {
            SpotifyStatusDot.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            SpotifyAuthorizeBtn.IsEnabled = false;
            SpotifyDisconnectBtn.IsEnabled = true;
        }
        else
        {
            SpotifyStatusDot.Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            SpotifyAuthorizeBtn.IsEnabled = true;
            SpotifyDisconnectBtn.IsEnabled = false;
        }
    }

    private void UpdateStreamlabsUI()
    {
        if (_vm.IsStreamlabsConnected)
        {
            StreamlabsStatusDot.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            StreamlabsConnectBtn.Content = "Disconnect";
        }
        else
        {
            StreamlabsStatusDot.Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            StreamlabsConnectBtn.Content = "Connect";
        }
    }

    private void OnStreamlabsPasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.StreamlabsPassword = StreamlabsPasswordBox.Password;
    }

    private async void OnStreamlabsConnectClick(object sender, RoutedEventArgs e)
    {
        _vm.StreamlabsPassword = StreamlabsPasswordBox.Password;

        if (_vm.IsStreamlabsConnected)
        {
            await _vm.DisconnectStreamlabsCommand.ExecuteAsync(null);
        }
        else
        {
            await _vm.ConnectStreamlabsCommand.ExecuteAsync(null);
        }
        UpdateStreamlabsUI();
    }

    private void UpdateDiscordUI()
    {
        var connected = _vm.DiscordStatus == "Connected";
        DiscordStatusDot.Fill = new SolidColorBrush(
            connected ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        DiscordConnectBtn.Content = connected ? "Disconnect" : "Connect";
    }

    private async void OnDiscordConnectClick(object sender, RoutedEventArgs e)
    {
        await _vm.ConnectDiscordCommand.ExecuteAsync(null);
        UpdateDiscordUI();
    }
}
