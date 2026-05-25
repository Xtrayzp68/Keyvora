namespace Keyvora.Desktop.UI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using Newtonsoft.Json;
using Keyvora.Desktop.Actions;
using Keyvora.Desktop.Actions.BuiltIn;
using Keyvora.Desktop.Services;

public sealed partial class ActionEditorViewModel : ObservableObject
{
    private readonly ActionRegistry _actionRegistry;
    private readonly StreamlabsService? _streamlabsService;

    public ObservableCollection<ActionItem> AvailableActions { get; } = new();

    [ObservableProperty]
    private ActionItem? _selectedAction;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Config properties for each action type
    [ObservableProperty]
    private string _configLabel = string.Empty;

    [ObservableProperty]
    private string _configFilePath = string.Empty;

    [ObservableProperty]
    private string _configKeyboardKeys = string.Empty;

    [ObservableProperty]
    private string _configText = string.Empty;

    [ObservableProperty]
    private string _configSpotifyCommand = "Play/Pause";

    public ObservableCollection<string> SpotifyCommands { get; } = new()
    {
        "Play/Pause",
        "Next Track",
        "Previous Track",
        "Volume Up",
        "Volume Down"
    };

    [ObservableProperty]
    private string _configSoundFilePath = string.Empty;

    [ObservableProperty]
    private int _configSoundDeviceId = -1;

    public ObservableCollection<AudioDeviceItem> AudioDevices { get; } = new();

    // Streamlabs config
    [ObservableProperty]
    private string _configStreamlabsCommand = "Switch Scene";

    [ObservableProperty]
    private string _configStreamlabsSceneName = string.Empty;

    [ObservableProperty]
    private string _configStreamlabsSourceName = string.Empty;

    public ObservableCollection<string> StreamlabsCommands { get; } = new()
    {
        "Switch Scene",
        "Toggle Source",
        "Start Stream",
        "Stop Stream",
        "Toggle Stream",
        "Start Recording",
        "Stop Recording",
        "Toggle Recording"
    };

    public ObservableCollection<string> StreamlabsScenes { get; } = new();
    public ObservableCollection<string> StreamlabsSources { get; } = new();

    [ObservableProperty]
    private string _configDiscordCommand = "Toggle Mute";

    public ObservableCollection<string> DiscordCommands { get; } = new()
    {
        "Toggle Mute",
        "Mute",
        "Unmute",
        "Toggle Deafen",
        "Deafen",
        "Undeafen",
        "Leave Voice Channel"
    };

    public async Task RefreshStreamlabsScenesAsync()
    {
        if (_streamlabsService == null || !_streamlabsService.IsConnected) return;

        try
        {
            var scenes = await _streamlabsService.GetSceneListAsync();
            StreamlabsScenes.Clear();
            foreach (var scene in scenes)
                StreamlabsScenes.Add(scene);
        }
        catch { }
    }

    public async Task RefreshStreamlabsSourcesAsync()
    {
        if (_streamlabsService == null || !_streamlabsService.IsConnected ||
            string.IsNullOrWhiteSpace(ConfigStreamlabsSceneName)) return;

        try
        {
            var sources = await _streamlabsService.GetSceneSourcesAsync(ConfigStreamlabsSceneName);
            StreamlabsSources.Clear();
            foreach (var source in sources)
                StreamlabsSources.Add(source);
        }
        catch { }
    }

    public void LoadAudioDevices()
    {
        AudioDevices.Clear();
        AudioDevices.Add(new AudioDeviceItem { Id = -1, Name = "Default Output Device" });
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            AudioDevices.Add(new AudioDeviceItem { Id = i, Name = caps.ProductName });
        }
    }

    public void LoadExistingConfig(string typeId, string? configJson)
    {
        ConfigLabel = string.Empty;
        ConfigFilePath = string.Empty;
        ConfigKeyboardKeys = string.Empty;
        ConfigText = string.Empty;
        ConfigSoundFilePath = string.Empty;
        ConfigSoundDeviceId = -1;
        ConfigStreamlabsCommand = "Switch Scene";
        ConfigStreamlabsSceneName = string.Empty;
        ConfigStreamlabsSourceName = string.Empty;
        ConfigDiscordCommand = "Toggle Mute";

        if (string.IsNullOrWhiteSpace(configJson))
            return;

        try
        {
            switch (typeId)
            {
                case "builtin.openfile":
                    var fileCfg = JsonConvert.DeserializeObject<OpenFileConfig>(configJson);
                    if (fileCfg != null)
                        ConfigFilePath = fileCfg.FilePath;
                    break;
                case "builtin.keyboard":
                    var kbCfg = JsonConvert.DeserializeObject<KeyboardShortcutConfig>(configJson);
                    if (kbCfg != null)
                        ConfigKeyboardKeys = kbCfg.Keys;
                    break;
                case "builtin.text":
                    var txtCfg = JsonConvert.DeserializeObject<TextActionConfig>(configJson);
                    if (txtCfg != null)
                        ConfigText = txtCfg.Text;
                    break;
                case "builtin.playsound":
                    var sndCfg = JsonConvert.DeserializeObject<PlaySoundConfig>(configJson);
                    if (sndCfg != null)
                    {
                        ConfigSoundFilePath = sndCfg.FilePath;
                        ConfigSoundDeviceId = sndCfg.OutputDeviceId;
                    }
                    break;
                case "builtin.discord":
                    var dCfg = JsonConvert.DeserializeObject<DiscordActionConfig>(configJson);
                    if (dCfg != null)
                    {
                        ConfigDiscordCommand = dCfg.Command switch
                        {
                            "togglemute" => "Toggle Mute",
                            "mute" => "Mute",
                            "unmute" => "Unmute",
                            "toggledeafen" => "Toggle Deafen",
                            "deafen" => "Deafen",
                            "undeafen" => "Undeafen",
                            "leavevoice" => "Leave Voice Channel",
                            _ => "Toggle Mute"
                        };
                    }
                    break;
                case "builtin.streamlabs":
                    var slCfg = JsonConvert.DeserializeObject<StreamlabsActionConfig>(configJson);
                    if (slCfg != null)
                    {
                        ConfigStreamlabsCommand = slCfg.Command switch
                        {
                            "switchscene" => "Switch Scene",
                            "togglesource" => "Toggle Source",
                            "startstream" => "Start Stream",
                            "stopstream" => "Stop Stream",
                            "togglestream" => "Toggle Stream",
                            "startrecord" => "Start Recording",
                            "stoprecord" => "Stop Recording",
                            "togglerecord" => "Toggle Recording",
                            _ => "Switch Scene"
                        };
                        ConfigStreamlabsSceneName = slCfg.SceneName;
                        ConfigStreamlabsSourceName = slCfg.SourceName;
                    }
                    break;
            }
        }
        catch
        {
            // ignore deserialization errors
        }
    }

    public ActionEditorViewModel(ActionRegistry actionRegistry, StreamlabsService? streamlabsService = null)
    {
        _actionRegistry = actionRegistry;
        _streamlabsService = streamlabsService;
    }

    public void LoadAvailableActions(IEnumerable<IAction> actions)
    {
        AvailableActions.Clear();
        foreach (var action in actions)
        {
            AvailableActions.Add(new ActionItem
            {
                TypeId = action.TypeId,
                DisplayName = action.DisplayName,
                Description = action.Description,
                Category = GetCategoryFromTypeId(action.TypeId)
            });
        }
    }

    private static string GetCategoryFromTypeId(string typeId)
    {
        return typeId.StartsWith("builtin.", StringComparison.Ordinal) ? "Built-In" : "Plugins";
    }
}

public sealed class ActionItem
{
    public string TypeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";

    public override string ToString() => DisplayName;
}

public sealed class AudioDeviceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
