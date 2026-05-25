namespace Keyvora.Desktop.UI.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Keyvora.Desktop.Profiles;
using Keyvora.Desktop.UI.Views;

public sealed partial class ProfileSelectorViewModel : ObservableObject
{
    private readonly ProfileManager _profileManager;

    public ObservableCollection<ProfileItem> Profiles { get; } = new();

    [ObservableProperty]
    private ProfileItem? _selectedProfile;

    public event Action? ProfileActivated;

    public ProfileSelectorViewModel(ProfileManager profileManager)
    {
        _profileManager = profileManager;
        _profileManager.ProfileActivated += OnProfileActivated;
    }

    private void OnProfileActivated(Profile profile)
    {
        ProfileActivated?.Invoke();
    }

    public void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _profileManager.Profiles)
        {
            Profiles.Add(new ProfileItem
            {
                Id = profile.Id,
                Name = profile.Name,
                IsActive = profile.IsActive
            });
        }
    }

    private static string LogPath => Path.Combine(Path.GetTempPath(), "Keyvora_ProfileTrace.log");
    private static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}"); }
        catch { }
    }

    [RelayCommand]
    private void SelectProfile(ProfileItem? item)
    {
        if (item == null)
        {
            Log("SelectProfile: item is null");
            return;
        }
        Log($"SelectProfile: clicked profile='{item.Name}' id='{item.Id}' IsActive={item.IsActive}");
        Log($"SelectProfile: BEFORE ActivateProfile - ActiveProfile='{_profileManager.ActiveProfile?.Name}' IsActive={_profileManager.ActiveProfile?.IsActive}");

        var result = _profileManager.ActivateProfile(item.Id);
        Log($"SelectProfile: AFTER ActivateProfile result={result} - ActiveProfile='{_profileManager.ActiveProfile?.Name}' IsActive={_profileManager.ActiveProfile?.IsActive}");

        Log("SelectProfile: before UpdateActiveState - Profiles state:");
        foreach (var pi in Profiles)
            Log($"  - {pi.Name}: IsActive={pi.IsActive}");

        UpdateActiveState(item);

        Log("SelectProfile: after UpdateActiveState - Profiles state:");
        foreach (var pi in Profiles)
            Log($"  - {pi.Name}: IsActive={pi.IsActive}");
    }

    private void UpdateActiveState(ProfileItem activeItem)
    {
        foreach (var pi in Profiles)
        {
            var newValue = pi.Id == activeItem.Id;
            Log($"UpdateActiveState: {pi.Name}: IsActive {pi.IsActive} -> {newValue}");
            pi.IsActive = newValue;
        }
    }

    public void CreateNewProfile()
    {
        var count = _profileManager.Profiles.Count;
        var profile = _profileManager.AddProfile($"Profil {count + 1}");
        _profileManager.ActivateProfile(profile.Id);
        LoadProfiles();
    }

    [RelayCommand]
    private void RenameProfile(ProfileItem? item)
    {
        if (item == null) return;

        var dialog = new RenameDialog(item.Name);
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            var profile = _profileManager.Profiles.FirstOrDefault(p => p.Id == item.Id);
            if (profile != null)
            {
                profile.Name = dialog.Result;
                _profileManager.SaveProfile(profile);
                LoadProfiles();
            }
        }
    }

    [RelayCommand]
    private void DeleteProfile(ProfileItem? item)
    {
        if (item == null) return;

        if (_profileManager.Profiles.Count <= 1)
        {
            MessageBox.Show(Application.Current.MainWindow,
                "Impossible de supprimer le dernier profil.",
                "Suppression", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(Application.Current.MainWindow,
            $"Supprimer le profil \"{item.Name}\" ?",
            "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _profileManager.RemoveProfile(item.Id);
            LoadProfiles();
        }
    }
}

public sealed partial class ProfileItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isActive;
}
