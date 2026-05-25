namespace Keyvora.Desktop.Profiles;

using System.Collections.Generic;

public interface IProfileStore
{
    IReadOnlyList<Profile> Profiles { get; }
    Profile? ActiveProfile { get; }
    event Action<Profile>? ProfileActivated;
    event Action<Profile>? ProfileAdded;
    event Action<string>? ProfileRemoved;
    void LoadProfiles();
    void SaveProfiles();
    Profile AddProfile(string name);
    void RemoveProfile(string profileId);
    bool ActivateProfile(string profileId);
}
