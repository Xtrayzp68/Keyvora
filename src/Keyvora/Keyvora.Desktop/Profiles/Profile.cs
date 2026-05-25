namespace Keyvora.Desktop.Profiles;

using System.Collections.Generic;
using Newtonsoft.Json;
using Keyvora.Desktop.Actions;

public sealed class Profile
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonProperty("name")]
    public string Name { get; set; } = "Profil 1";

    [JsonProperty("buttons")]
    public Dictionary<int, ButtonMapping> Buttons { get; set; } = new();

    [JsonProperty("isActive")]
    public bool IsActive { get; set; }

    [JsonProperty("gridColumns")]
    public int GridColumns { get; set; } = 3;

    [JsonProperty("gridRows")]
    public int GridRows { get; set; } = 2;

    [JsonProperty("icon")]
    public string? Icon { get; set; }

    public Profile Clone()
    {
        var json = JsonConvert.SerializeObject(this);
        return JsonConvert.DeserializeObject<Profile>(json)!;
    }
}
