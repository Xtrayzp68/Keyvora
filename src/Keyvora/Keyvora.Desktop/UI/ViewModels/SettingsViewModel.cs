namespace Keyvora.Desktop.UI.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed partial class SettingsViewModel : ObservableObject
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Keyvora", "settings.json");

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Keyvora.Desktop";

    public ObservableCollection<string> AvailablePorts { get; } = new();

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private bool _autoConnect = true;

    [ObservableProperty]
    private int _baudRate = 115200;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _autoSaveProfiles = true;

    [ObservableProperty]
    private bool _enablePlugins = true;

    [ObservableProperty]
    private bool _launchAtStartup;

    public SettingsViewModel()
    {
        Load();
        RefreshPorts();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var data = JObject.Parse(json);

            AutoConnect = data["autoConnect"]?.Value<bool>() ?? true;
            AutoSaveProfiles = data["autoSaveProfiles"]?.Value<bool>() ?? true;
            EnablePlugins = data["enablePlugins"]?.Value<bool>() ?? true;
            StartMinimized = data["startMinimized"]?.Value<bool>() ?? false;
            LaunchAtStartup = data["launchAtStartup"]?.Value<bool>() ?? false;
            BaudRate = data["baudRate"]?.Value<int>() ?? 115200;
            SelectedPort = data["selectedPort"]?.Value<string>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Failed to load: {ex.Message}");
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var data = new JObject
            {
                ["autoConnect"] = AutoConnect,
                ["autoSaveProfiles"] = AutoSaveProfiles,
                ["enablePlugins"] = EnablePlugins,
                ["startMinimized"] = StartMinimized,
                ["launchAtStartup"] = LaunchAtStartup,
                ["baudRate"] = BaudRate,
                ["selectedPort"] = SelectedPort ?? ""
            };

            File.WriteAllText(SettingsPath, data.ToString(Formatting.Indented));

            SetAutoStart(LaunchAtStartup);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Failed to save: {ex.Message}");
        }
    }

    private static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                if (key.GetValue(AppName) != null)
                    key.DeleteValue(AppName);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Failed to set auto-start: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        AvailablePorts.Clear();
        foreach (var port in SerialPort.GetPortNames())
        {
            AvailablePorts.Add(port);
        }
    }
}
