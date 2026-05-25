namespace Keyvora.Desktop;

using System.Diagnostics;
using System.IO;
using System.Windows;
using Newtonsoft.Json.Linq;
using Keyvora.Desktop.Actions;
using Keyvora.Desktop.Actions.BuiltIn;
using Keyvora.Desktop.Events;
using Keyvora.Desktop.Hardware;
using Keyvora.Desktop.Plugins;
using Keyvora.Desktop.Profiles;
using Keyvora.Desktop.Services;
using Keyvora.Desktop.UI.ViewModels;
using Keyvora.Desktop.UI.Views;

public partial class App : Application
{
    private readonly IEventBus _eventBus = new EventBus();
    private readonly ActionRegistry _actionRegistry = new();
    private IDeviceManager? _deviceManager;
    private ProfileManager? _profileManager;
    private SpotifyService? _spotifyService;
    private StreamlabsService? _streamlabsService;
    private DiscordService? _discordService;
    private PluginLoader? _pluginLoader;

    public new MainWindow? MainWindow => Current.MainWindow as MainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show(
                (args.ExceptionObject as Exception)?.ToString() ?? "Unknown error",
                "Unhandled Exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                args.Exception.ToString(),
                "Dispatcher Exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        RegisterBuiltInActions();
        InitializeServices();
        LoadPlugins();

        var viewModel = CreateMainViewModel();
        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();

        if (_deviceManager != null)
        {
            _ = _deviceManager.AutoConnectAsync();
        }

        if (_streamlabsService != null && !string.IsNullOrEmpty(_streamlabsService.Password))
        {
            _ = AutoConnectStreamlabsAsync();
        }

        if (_discordService != null && !string.IsNullOrEmpty(_discordService.ClientId))
        {
            _ = AutoConnectDiscordAsync();
        }
    }

    private async Task AutoConnectDiscordAsync()
    {
        try
        {
            await _discordService!.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Discord] Auto-connect failed: {ex.Message}");
        }
    }

    private async Task AutoConnectStreamlabsAsync()
    {
        try
        {
            await _streamlabsService!.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Streamlabs] Auto-connect failed: {ex.Message}");
        }
    }

    private void RegisterBuiltInActions()
    {
        _actionRegistry.Register(new KeyboardShortcutAction());
        _actionRegistry.Register(new OpenFileAction());
        _actionRegistry.Register(new TextAction());

        var macro = new MacroAction();
        MacroAction.Initialize(_actionRegistry);
        _actionRegistry.Register(macro);

        _actionRegistry.Register(new PlaySoundAction());
    }

    private void InitializeServices()
    {
        var profilesDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Keyvora", "profiles");

        _profileManager = new ProfileManager(profilesDir);
        _profileManager.LoadProfiles();

        _deviceManager = new SerialDeviceManager(_eventBus);

        _spotifyService = LoadSpotifyService();

        SpotifyAction.Initialize(_spotifyService);
        _actionRegistry.Register(new SpotifyAction());

        _streamlabsService = new StreamlabsService();
        _streamlabsService.LoadConnectionInfo();
        StreamlabsAction.Initialize(_streamlabsService);
        _actionRegistry.Register(new StreamlabsAction());

        _discordService = new DiscordService();
        _discordService.LoadConfig();
        DiscordAction.Initialize(_discordService);
        _actionRegistry.Register(new DiscordAction());
    }

    private static SpotifyService LoadSpotifyService()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        var clientId = "YOUR_SPOTIFY_CLIENT_ID";
        var clientSecret = "YOUR_SPOTIFY_CLIENT_SECRET";
        var redirectUri = "http://localhost:8888/callback";

        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var data = JObject.Parse(json);
                var spotify = data["Spotify"];
                if (spotify != null)
                {
                    clientId = spotify["ClientId"]?.ToString() ?? clientId;
                    clientSecret = spotify["ClientSecret"]?.ToString() ?? clientSecret;
                    redirectUri = spotify["RedirectUri"]?.ToString() ?? redirectUri;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Config] Failed to read appsettings.json: {ex.Message}");
        }

        return new SpotifyService(clientId, clientSecret, redirectUri);
    }

    private void LoadPlugins()
    {
        var pluginsDir = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "plugins");

        var context = new PluginContext(
            _eventBus,
            _profileManager!,
            pluginsDir,
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Keyvora", "plugin-data"));

        _pluginLoader = new PluginLoader(pluginsDir, context, _actionRegistry);
        _pluginLoader.LoadAllPlugins();
    }

    private MainViewModel CreateMainViewModel()
    {
        return new MainViewModel(
            _eventBus,
            _deviceManager!,
            _profileManager!,
            _actionRegistry,
            _spotifyService,
            _streamlabsService,
            _discordService);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _profileManager?.SaveProfiles();
        _pluginLoader?.Dispose();
        _deviceManager?.Dispose();
        _spotifyService?.Dispose();
        _streamlabsService?.Dispose();
        _discordService?.Dispose();
        base.OnExit(e);
    }
}
