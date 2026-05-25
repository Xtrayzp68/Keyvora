namespace Keyvora.Desktop.UI.ViewModels;

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Keyvora.Desktop.Services;

public sealed partial class PluginsViewModel : ObservableObject
{
    private readonly SpotifyService? _spotifyService;
    private readonly StreamlabsService? _streamlabsService;
    private readonly DiscordService? _discordService;
    private bool _isConnecting;

    public PluginsViewModel(SpotifyService? spotifyService, StreamlabsService? streamlabsService,
                            DiscordService? discordService = null)
    {
        _spotifyService = spotifyService;
        _streamlabsService = streamlabsService;
        _discordService = discordService;

        if (_spotifyService != null)
        {
            IsSpotifyAuthorized = _spotifyService.IsAuthorized;
            SpotifyStatus = _spotifyService.IsAuthorized ? "Authorized" : "Not authorized";
        }
        else
        {
            SpotifyStatus = "Service not available";
        }

        if (_streamlabsService != null)
        {
            IsStreamlabsConnected = _streamlabsService.IsConnected;
            StreamlabsConnectionStatus = _streamlabsService.IsConnected ? "Connected" : "Disconnected";
            StreamlabsHost = _streamlabsService.Host;
            StreamlabsPort = _streamlabsService.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            StreamlabsPassword = _streamlabsService.Password ?? "";
        }

        if (_discordService != null)
        {
            DiscordClientId = _discordService.ClientId ?? "";
            DiscordStatus = _discordService.IsConnected ? "Connected" : "Disconnected";
        }
        else
        {
            DiscordStatus = "Service not available";
        }
    }

    public event Action? SpotifyAuthorizationCompleted;
    public event Action? SpotifyAuthorizationFailed;

    [ObservableProperty]
    private bool _isSpotifyAuthorized;

    [ObservableProperty]
    private string _spotifyStatus = "Not authorized";

    [ObservableProperty]
    private string _streamlabsHost = "localhost";

    [ObservableProperty]
    private string _streamlabsPort = "59650";

    [ObservableProperty]
    private string _streamlabsPassword = string.Empty;

    [ObservableProperty]
    private string _streamlabsConnectionStatus = "Disconnected";

    [ObservableProperty]
    private bool _isStreamlabsConnected;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AuthorizeSpotify(CancellationToken ct)
    {
        if (_spotifyService == null)
        {
            SpotifyStatus = "Service not available";
            return;
        }

        if (!_spotifyService.HasValidCredentials)
        {
            SpotifyStatus = "Configure ClientId/Secret in appsettings.json";
            return;
        }

        SpotifyStatus = "Waiting for authorization...";

        try
        {
            await Task.Run(async () =>
            {
                var uri = new Uri(_spotifyService.RedirectUri);
                var prefix = $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath.TrimEnd('/')}/";

                using var listener = new HttpListener();
                listener.Prefixes.Add(prefix);
                try { listener.Start(); }
                catch
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        SpotifyStatus = $"Cannot listen on {uri.Host}:{uri.Port}");
                    return;
                }

                var authUrl = _spotifyService.GetAuthorizationUrl();
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                var context = await listener.GetContextAsync();
                var code = context.Request.QueryString["code"];
                var error = context.Request.QueryString["error"];

                var response = context.Response;
                if (!string.IsNullOrEmpty(error))
                {
                    var errBytes = System.Text.Encoding.UTF8.GetBytes(
                        "<html><body><h2>Authorization denied</h2><p>Close this page.</p></body></html>");
                    response.ContentType = "text/html; charset=utf-8";
                    await response.OutputStream.WriteAsync(errBytes);
                    response.Close();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SpotifyStatus = "Authorization denied";
                        SpotifyAuthorizationFailed?.Invoke();
                    });
                    return;
                }

                if (string.IsNullOrEmpty(code))
                {
                    var errBytes = System.Text.Encoding.UTF8.GetBytes(
                        "<html><body><h2>Error</h2><p>No code received.</p></body></html>");
                    response.ContentType = "text/html; charset=utf-8";
                    await response.OutputStream.WriteAsync(errBytes);
                    response.Close();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SpotifyStatus = "OAuth error";
                        SpotifyAuthorizationFailed?.Invoke();
                    });
                    return;
                }

                var ok = await _spotifyService.AuthorizeAsync(code);
                var successBytes = System.Text.Encoding.UTF8.GetBytes(
                    ok
                        ? "<html><body><h2>Authorization successful!</h2><p>Close this page.</p></body></html>"
                        : "<html><body><h2>Authorization failed</h2><p>Close and try again.</p></body></html>");
                response.ContentType = "text/html; charset=utf-8";
                await response.OutputStream.WriteAsync(successBytes);
                response.Close();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsSpotifyAuthorized = ok;
                    if (ok)
                    {
                        SpotifyStatus = "Authorized";
                        SpotifyAuthorizationCompleted?.Invoke();
                    }
                    else
                    {
                        SpotifyStatus = "Authorization failed";
                        SpotifyAuthorizationFailed?.Invoke();
                    }
                });
            }, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Spotify] Auth error: {ex.Message}");
            SpotifyStatus = $"Error: {ex.Message}";
            SpotifyAuthorizationFailed?.Invoke();
        }
    }

    [RelayCommand]
    private void DisconnectSpotify()
    {
        IsSpotifyAuthorized = false;
        SpotifyStatus = "Not authorized";
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ConnectStreamlabs(CancellationToken ct)
    {
        if (_streamlabsService == null)
        {
            StreamlabsConnectionStatus = "Service not available";
            return;
        }

        if (_isConnecting) return;
        _isConnecting = true;

        StreamlabsConnectionStatus = "Connecting...";
        try
        {
            _streamlabsService.Host = StreamlabsHost;
            _streamlabsService.Port = int.TryParse(StreamlabsPort, out var p) ? p : 59650;
            _streamlabsService.Password = string.IsNullOrWhiteSpace(StreamlabsPassword) ? null : StreamlabsPassword;

            await _streamlabsService.ConnectAsync();
            _streamlabsService.SaveConnectionInfo();
            IsStreamlabsConnected = true;
            StreamlabsConnectionStatus = "Connected";
        }
        catch (OperationCanceledException)
        {
            IsStreamlabsConnected = false;
            StreamlabsConnectionStatus = "Cancelled";
        }
        catch (Exception ex)
        {
            IsStreamlabsConnected = false;
            StreamlabsConnectionStatus = $"Error: {ex.Message}";
        }
        finally
        {
            _isConnecting = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectStreamlabs()
    {
        if (_streamlabsService == null) return;
        await _streamlabsService.DisconnectAsync();
        IsStreamlabsConnected = false;
        StreamlabsConnectionStatus = "Disconnected";
    }

    [ObservableProperty]
    private string _discordClientId = string.Empty;

    [ObservableProperty]
    private string _discordStatus = "Disconnected";

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ConnectDiscord(CancellationToken ct)
    {
        if (_discordService == null)
        {
            DiscordStatus = "Service not available";
            return;
        }

        if (_discordService.IsConnected)
        {
            await _discordService.DisconnectAsync();
            DiscordStatus = "Disconnected";
            return;
        }

        _discordService.ClientId = string.IsNullOrWhiteSpace(DiscordClientId) ? null : DiscordClientId;

        if (string.IsNullOrEmpty(_discordService.ClientId))
        {
            DiscordStatus = "Enter a Client ID";
            return;
        }

        DiscordStatus = "Connecting...";
        try
        {
            await _discordService.ConnectAsync();
            _discordService.SaveConfig();
            DiscordStatus = "Connected";
        }
        catch (Exception ex)
        {
            DiscordStatus = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DisconnectDiscord()
    {
        if (_discordService == null) return;
        _ = _discordService.DisconnectAsync();
        DiscordStatus = "Disconnected";
    }
}
