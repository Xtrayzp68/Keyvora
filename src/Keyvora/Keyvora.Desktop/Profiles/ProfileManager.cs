namespace Keyvora.Desktop.Profiles;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public sealed class ProfileManager : IProfileStore
{
    private readonly string _profilesDirectory;
    private readonly List<Profile> _profiles = new();
    private Profile? _activeProfile;

    public IReadOnlyList<Profile> Profiles => _profiles.AsReadOnly();
    public Profile? ActiveProfile => _activeProfile;

    public event Action<Profile>? ProfileActivated;
    public event Action<Profile>? ProfileAdded;
    public event Action<string>? ProfileRemoved;

    public ProfileManager(string profilesDirectory)
    {
        _profilesDirectory = profilesDirectory;
        Directory.CreateDirectory(_profilesDirectory);
    }

    public void LoadProfiles()
    {
        _profiles.Clear();

        if (!Directory.Exists(_profilesDirectory)) return;

        foreach (var file in Directory.GetFiles(_profilesDirectory, "*.json"))
        {
            try
            {
                var profile = ProfileSerializer.DeserializeFromFile(file);
                if (profile != null)
                {
                    _profiles.Add(profile);
                    if (profile.IsActive)
                        _activeProfile = profile;
                }
            }
            catch { /* skip corrupted files */ }
        }

        // Ensure at least one default profile
        if (_profiles.Count == 0)
        {
            var defaultProfile = new Profile { IsActive = true };
            _profiles.Add(defaultProfile);
            _activeProfile = defaultProfile;
        }

        _activeProfile ??= _profiles[0];
    }

    public void SaveProfiles()
    {
        foreach (var profile in _profiles)
        {
            var path = GetProfilePath(profile.Id);
            ProfileSerializer.SerializeToFile(profile, path);
        }
    }

    public Profile AddProfile(string name)
    {
        var profile = new Profile
        {
            Name = name,
            IsActive = false
        };

        _profiles.Add(profile);
        ProfileAdded?.Invoke(profile);
        SaveProfile(profile);
        return profile;
    }

    public void RemoveProfile(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) return;

        _profiles.Remove(profile);
        var path = GetProfilePath(profileId);
        if (File.Exists(path)) File.Delete(path);

        ProfileRemoved?.Invoke(profileId);

        if (_activeProfile?.Id == profileId)
        {
            ActivateProfile(_profiles.FirstOrDefault()?.Id ?? string.Empty);
        }
    }

    public bool ActivateProfile(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) return false;

        if (_activeProfile != null)
            _activeProfile.IsActive = false;

        profile.IsActive = true;
        _activeProfile = profile;

        ProfileActivated?.Invoke(profile);
        SaveProfiles();
        return true;
    }

    public void UpdateButtonMapping(int buttonIndex, ButtonMapping mapping)
    {
        if (_activeProfile == null) return;
        _activeProfile.Buttons.Update(buttonIndex, mapping);
        SaveProfile(_activeProfile);
    }

    public void RemoveButtonMapping(int buttonIndex)
    {
        if (_activeProfile == null) return;
        _activeProfile.Buttons.Remove(buttonIndex);
        SaveProfile(_activeProfile);
    }

    public void SwapButtonMappings(int fromIndex, int toIndex)
    {
        if (_activeProfile == null) return;

        _activeProfile.Buttons.TryGetValue(fromIndex, out var fromMapping);
        _activeProfile.Buttons.TryGetValue(toIndex, out var toMapping);

        if (fromMapping != null)
            _activeProfile.Buttons[toIndex] = fromMapping;
        else
            _activeProfile.Buttons.Remove(toIndex);

        if (toMapping != null)
            _activeProfile.Buttons[fromIndex] = toMapping;
        else
            _activeProfile.Buttons.Remove(fromIndex);

        SaveProfile(_activeProfile);
    }

    public void ReorderProfile(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _profiles.Count ||
            newIndex < 0 || newIndex >= _profiles.Count)
            return;

        var profile = _profiles[oldIndex];
        _profiles.RemoveAt(oldIndex);
        _profiles.Insert(newIndex, profile);
        SaveProfiles();
    }

    public ButtonMapping? GetButtonMapping(int buttonIndex)
    {
        if (_activeProfile?.Buttons.TryGetValue(buttonIndex, out var mapping) == true)
            return mapping;
        return null;
    }

    public void SaveProfile(Profile profile)
    {
        var path = GetProfilePath(profile.Id);
        ProfileSerializer.SerializeToFile(profile, path);
    }

    private string GetProfilePath(string profileId) =>
        Path.Combine(_profilesDirectory, $"profile_{profileId}.json");
}
