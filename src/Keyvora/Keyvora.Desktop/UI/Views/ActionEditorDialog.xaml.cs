namespace Keyvora.Desktop.UI.Views;

using System.Windows;
using Microsoft.Win32;
using Keyvora.Desktop.Actions;
using Keyvora.Desktop.Actions.BuiltIn;
using Keyvora.Desktop.Services;
using Keyvora.Desktop.UI.ViewModels;

public partial class ActionEditorDialog : Window
{
    private readonly ActionEditorViewModel _editorVm;
    private readonly ButtonViewModel _buttonVm;

    public ActionEditorDialog(ActionEditorViewModel editorVm, ButtonViewModel buttonVm)
    {
        InitializeComponent();
        _editorVm = editorVm;
        _buttonVm = buttonVm;
        DataContext = _editorVm;
    }

    private void OnActionSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        PanelOpenFile.Visibility = Visibility.Collapsed;
        PanelKeyboard.Visibility = Visibility.Collapsed;
        PanelText.Visibility = Visibility.Collapsed;
        PanelSpotify.Visibility = Visibility.Collapsed;
        PanelMacro.Visibility = Visibility.Collapsed;
        PanelPlaySound.Visibility = Visibility.Collapsed;
        PanelDiscord.Visibility = Visibility.Collapsed;
        PanelStreamlabs.Visibility = Visibility.Collapsed;

        if (_editorVm.SelectedAction == null)
        {
            ConfigPanel.Visibility = Visibility.Collapsed;
            return;
        }

        ConfigPanel.Visibility = Visibility.Visible;

        switch (_editorVm.SelectedAction.TypeId)
        {
            case "builtin.openfile":
                PanelOpenFile.Visibility = Visibility.Visible;
                break;
            case "builtin.keyboard":
                PanelKeyboard.Visibility = Visibility.Visible;
                break;
            case "builtin.text":
                PanelText.Visibility = Visibility.Visible;
                break;
            case "builtin.spotify":
                PanelSpotify.Visibility = Visibility.Visible;
                break;
            case "builtin.macro":
                PanelMacro.Visibility = Visibility.Visible;
                break;
            case "builtin.playsound":
                _editorVm.LoadAudioDevices();
                PanelPlaySound.Visibility = Visibility.Visible;
                break;
            case "builtin.discord":
                PanelDiscord.Visibility = Visibility.Visible;
                break;
            case "builtin.streamlabs":
                PanelStreamlabs.Visibility = Visibility.Visible;
                UpdateStreamlabsCommandUI();
                _ = RefreshStreamlabsSceneListAsync();
                break;
        }
    }

    private void OnBrowseFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select File",
            Multiselect = false,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            _editorVm.ConfigFilePath = dialog.FileName;
        }
    }

    private void OnBrowseSoundFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Sound File",
            Multiselect = false,
            CheckFileExists = true,
            Filter = "Audio Files (*.mp3;*.wav;*.ogg;*.flac;*.aac;*.wma)|*.mp3;*.wav;*.ogg;*.flac;*.aac;*.wma|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            _editorVm.ConfigSoundFilePath = dialog.FileName;
        }
    }

    private void OnStreamlabsCommandChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateStreamlabsCommandUI();
    }

    private async void OnStreamlabsSceneChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        await _editorVm.RefreshStreamlabsSourcesAsync();
    }

    private void UpdateStreamlabsCommandUI()
    {
        var cmd = _editorVm.ConfigStreamlabsCommand;
        var needsScene = cmd is "Switch Scene" or "Toggle Source";
        var needsSource = cmd is "Toggle Source";

        StreamlabsSceneLabel.Visibility = needsScene ? Visibility.Visible : Visibility.Collapsed;
        StreamlabsSceneCombo.Visibility = needsScene ? Visibility.Visible : Visibility.Collapsed;
        StreamlabsSourceLabel.Visibility = needsSource ? Visibility.Visible : Visibility.Collapsed;
        StreamlabsSourceCombo.Visibility = needsSource ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task RefreshStreamlabsSceneListAsync()
    {
        var prevScene = _editorVm.ConfigStreamlabsSceneName;
        await _editorVm.RefreshStreamlabsScenesAsync();

        if (!string.IsNullOrWhiteSpace(prevScene) && _editorVm.StreamlabsScenes.Contains(prevScene))
            _editorVm.ConfigStreamlabsSceneName = prevScene;
    }

    private void OnAssignClick(object sender, RoutedEventArgs e)
    {
        if (_editorVm.SelectedAction == null)
        {
            MessageBox.Show("Please select an action.", "Keyvora",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Serialize config
        string? configJson = null;
        var typeId = _editorVm.SelectedAction.TypeId;

        switch (typeId)
        {
            case "builtin.openfile":
                if (!string.IsNullOrWhiteSpace(_editorVm.ConfigFilePath))
                {
                    configJson = new OpenFileConfig { FilePath = _editorVm.ConfigFilePath }.Serialize();
                }
                break;
            case "builtin.keyboard":
                if (!string.IsNullOrWhiteSpace(_editorVm.ConfigKeyboardKeys))
                {
                    configJson = new KeyboardShortcutConfig { Keys = _editorVm.ConfigKeyboardKeys }.Serialize();
                }
                break;
            case "builtin.text":
                if (!string.IsNullOrWhiteSpace(_editorVm.ConfigText))
                {
                    configJson = new TextActionConfig { Text = _editorVm.ConfigText }.Serialize();
                }
                break;
            case "builtin.spotify":
                var spotifyCmd = _editorVm.ConfigSpotifyCommand switch
                {
                    "Play/Pause" => "playpause",
                    "Next Track" => "next",
                    "Previous Track" => "previous",
                    "Volume Up" => "volumeup",
                    "Volume Down" => "volumedown",
                    _ => "playpause"
                };
                configJson = new SpotifyActionConfig { Command = spotifyCmd }.Serialize();
                break;
            case "builtin.playsound":
                if (!string.IsNullOrWhiteSpace(_editorVm.ConfigSoundFilePath))
                {
                    configJson = new PlaySoundConfig
                    {
                        FilePath = _editorVm.ConfigSoundFilePath,
                        OutputDeviceId = _editorVm.ConfigSoundDeviceId
                    }.Serialize();
                }
                break;
            case "builtin.discord":
                var dCommand = _editorVm.ConfigDiscordCommand switch
                {
                    "Toggle Mute" => "togglemute",
                    "Mute" => "mute",
                    "Unmute" => "unmute",
                    "Toggle Deafen" => "toggledeafen",
                    "Deafen" => "deafen",
                    "Undeafen" => "undeafen",
                    "Leave Voice Channel" => "leavevoice",
                    _ => "togglemute"
                };
                configJson = new DiscordActionConfig { Command = dCommand }.Serialize();
                break;
            case "builtin.streamlabs":
                var slCommand = _editorVm.ConfigStreamlabsCommand switch
                {
                    "Switch Scene" => "switchscene",
                    "Toggle Source" => "togglesource",
                    "Start Stream" => "startstream",
                    "Stop Stream" => "stopstream",
                    "Toggle Stream" => "togglestream",
                    "Start Recording" => "startrecord",
                    "Stop Recording" => "stoprecord",
                    "Toggle Recording" => "togglerecord",
                    _ => "switchscene"
                };
                configJson = new StreamlabsActionConfig
                {
                    Command = slCommand,
                    SceneName = _editorVm.ConfigStreamlabsSceneName ?? "",
                    SourceName = _editorVm.ConfigStreamlabsSourceName ?? ""
                }.Serialize();
                break;
        }

        if (typeId is "builtin.keyboard" or "builtin.openfile" or "builtin.text" or "builtin.playsound")
        {
            if (configJson == null)
            {
                MessageBox.Show("Please configure the action before assigning.", "Keyvora",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        _buttonVm.AssignAction(typeId, customLabel: null);
        _buttonVm.IsSelected = false;

        Tag = configJson;

        DialogResult = true;
        Close();
    }
}
