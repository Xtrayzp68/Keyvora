namespace Keyvora.Desktop.UI.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Keyvora.Desktop.Actions;
using Keyvora.Desktop.Events;
using Keyvora.Desktop.Hardware;
using Keyvora.Desktop.Profiles;
using Keyvora.Desktop.Services;
using Keyvora.Desktop.UI.Views;
using System.IO;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IEventBus _eventBus;
    private readonly IDeviceManager _deviceManager;
    private readonly ProfileManager _profileManager;
    private readonly ActionRegistry _actionRegistry;
    private readonly SpotifyService? _spotifyService;
    private readonly StreamlabsService? _streamlabsService;
    private readonly DiscordService? _discordService;
    private CancellationTokenSource? _spotifyPollCts;

    // Subscriptions
    private readonly List<IDisposable> _subscriptions = new();

    public event Action<int, int>? GridDimensionsChanged;

    public MainViewModel(
        IEventBus eventBus,
        IDeviceManager deviceManager,
        ProfileManager profileManager,
        ActionRegistry actionRegistry,
        SpotifyService? spotifyService = null,
        StreamlabsService? streamlabsService = null,
        DiscordService? discordService = null)
    {
        _eventBus = eventBus;
        _deviceManager = deviceManager;
        _profileManager = profileManager;
        _actionRegistry = actionRegistry;
        _spotifyService = spotifyService;
        _streamlabsService = streamlabsService;
        _discordService = discordService;

        ButtonGrid = new ButtonGridViewModel(3, 2, _actionRegistry);
        ProfileSelector = new ProfileSelectorViewModel(_profileManager);
        ActionEditor = new ActionEditorViewModel(_actionRegistry, _streamlabsService);

        SubscribeToEvents();
        ProfileSelector.ProfileActivated += OnProfileActivated;
        LoadProfiles();
        LoadActions();
        OnProfileActivated();

        if (_spotifyService != null)
        {
            _spotifyService.OnStatusMessage = msg => CurrentTrack = msg;
            if (_spotifyService.IsAuthorized)
                StartSpotifyPolling();
        }
    }

    [ObservableProperty]
    private bool _isDeviceConnected;

    [ObservableProperty]
    private string _deviceStatus = "Disconnected";

    [ObservableProperty]
    private string _currentTrack = string.Empty;

    [ObservableProperty]
    private ButtonGridViewModel _buttonGrid;

    [ObservableProperty]
    private ProfileSelectorViewModel _profileSelector;

    [ObservableProperty]
    private ActionEditorViewModel _actionEditor;

    [ObservableProperty]
    private bool _isSpotifyAuthorized;

    private void SubscribeToEvents()
    {
        _subscriptions.Add(_eventBus.Subscribe<DeviceConnectedEvent>(OnDeviceConnected));
        _subscriptions.Add(_eventBus.Subscribe<DeviceDisconnectedEvent>(OnDeviceDisconnected));
        _subscriptions.Add(_eventBus.Subscribe<ButtonPressedEvent>(OnButtonPressed));
    }

    private void OnDeviceConnected(DeviceConnectedEvent ev)
    {
        IsDeviceConnected = true;
        DeviceStatus = $"Connected - {ev.PortName}";
    }

    private void OnDeviceDisconnected(DeviceDisconnectedEvent ev)
    {
        IsDeviceConnected = false;
        DeviceStatus = "Disconnected";
    }

    private async void OnButtonPressed(ButtonPressedEvent ev)
    {
        if (_profileManager.ActiveProfile == null ||
            !_profileManager.ActiveProfile.Buttons.TryGetValue(ev.ButtonIndex, out var mapping))
            return;

        IAction action;
        try
        {
            action = _actionRegistry.Clone(mapping.ActionTypeId);
        }
        catch
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(mapping.ActionConfigJson) && action.Config != null)
        {
            action.Config.Deserialize(mapping.ActionConfigJson);
        }

        var context = new ActionContext(
            ev.ButtonIndex,
            _profileManager.ActiveProfile.Name);

        try
        {
            await action.ExecuteAsync(context);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Action execution error: {ex.Message}");
        }
    }

    private void LoadProfiles()
    {
        ProfileSelector.LoadProfiles();
    }

    public ProfileManager? GetProfileManager() => _profileManager;

    private void LoadActions()
    {
        ActionEditor.LoadAvailableActions(_actionRegistry.GetAll());
    }

    private void LoadButtons()
    {
        if (_profileManager.ActiveProfile == null) return;
        ButtonGrid.LoadFromProfile(_profileManager.ActiveProfile);
    }

    public void ClearButtonAction(int buttonIndex)
    {
        _profileManager.RemoveButtonMapping(buttonIndex);
        LoadButtons();
    }

    public void SwapButtons(int fromIndex, int toIndex)
    {
        _profileManager.SwapButtonMappings(fromIndex, toIndex);
        LoadButtons();
    }

    private static string LogPath => Path.Combine(Path.GetTempPath(), "Keyvora_ProfileTrace.log");
    private static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}"); }
        catch { }
    }

    private void OnProfileActivated()
    {
        Log($"OnProfileActivated: ActiveProfile='{_profileManager.ActiveProfile?.Name}'");
        if (_profileManager.ActiveProfile != null)
        {
            var p = _profileManager.ActiveProfile;
            Log($"OnProfileActivated: Resize({p.GridColumns}, {p.GridRows})");
            ButtonGrid.Resize(p.GridColumns, p.GridRows);
        }
        Log("OnProfileActivated: LoadButtons()");
        LoadButtons();
        Log($"OnProfileActivated: GridDimensionsChanged({ButtonGrid.GridColumns}, {ButtonGrid.GridRows})");
        GridDimensionsChanged?.Invoke(ButtonGrid.GridColumns, ButtonGrid.GridRows);
        Log("OnProfileActivated: done");
    }

    [RelayCommand]
    private async Task RefreshDevice()
    {
        DeviceStatus = "Scanning...";
        _deviceManager.Disconnect();
        var found = await _deviceManager.AutoConnectAsync();
        if (!found)
        {
            DeviceStatus = "Disconnected";
        }
    }

    private IRelayCommand? _configureGridCommand;
    public IRelayCommand ConfigureGridCommand => _configureGridCommand ??= new RelayCommand(ConfigureGrid);

    private void ConfigureGrid()
    {
        try
        {
            var dialog = new UI.Views.GridConfigDialog(ButtonGrid.GridColumns, ButtonGrid.GridRows);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                ButtonGrid.Resize(dialog.Columns, dialog.Rows);
                if (_profileManager.ActiveProfile != null)
                {
                    _profileManager.ActiveProfile.GridColumns = dialog.Columns;
                    _profileManager.ActiveProfile.GridRows = dialog.Rows;
                    _profileManager.SaveProfile(_profileManager.ActiveProfile);
                }
                LoadButtons();
                GridDimensionsChanged?.Invoke(dialog.Columns, dialog.Rows);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur: {ex.Message}", "GridConfig", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel();
        var window = new UI.Views.SettingsDialog();
        window.DataContext = vm;
        window.Owner = System.Windows.Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            vm.Save();

            if (vm.SelectedPort != null)
            {
                _deviceManager.Disconnect();
                _deviceManager.Connect(vm.SelectedPort);
            }
            if (!vm.AutoConnect && _deviceManager.IsConnected)
            {
                _deviceManager.Disconnect();
            }
        }
    }

    [RelayCommand]
    private void AddProfile()
    {
        ProfileSelector.CreateNewProfile();
        LoadButtons();
    }

    [RelayCommand]
    private void OpenPlugins()
    {
        var pluginsVm = new PluginsViewModel(_spotifyService, _streamlabsService, _discordService);
        pluginsVm.SpotifyAuthorizationCompleted += OnSpotifyAuthorized;
        pluginsVm.SpotifyAuthorizationFailed += () => IsSpotifyAuthorized = false;

        var dialog = new PluginsDialog(pluginsVm);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        dialog.ShowDialog();

        IsSpotifyAuthorized = _spotifyService?.IsAuthorized ?? false;
    }

    private void OnSpotifyAuthorized()
    {
        IsSpotifyAuthorized = true;
        StartSpotifyPolling();
    }

    private async void StartSpotifyPolling()
    {
        _spotifyPollCts?.Cancel();
        _spotifyPollCts = new CancellationTokenSource();
        var token = _spotifyPollCts.Token;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, token);
                if (_spotifyService == null || !_spotifyService.IsAuthorized) break;

                var track = await _spotifyService.GetCurrentTrackAsync();
                if (track != null && !token.IsCancellationRequested)
                {
                    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        CurrentTrack = track);
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Spotify] Poll error: {ex.Message}");
            }
        }
    }
}
