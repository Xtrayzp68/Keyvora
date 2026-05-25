namespace Keyvora.Desktop.Plugins;

using System.IO;
using Newtonsoft.Json;

public sealed class PluginManifest
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonProperty("author")]
    public string Author { get; set; } = "Unknown";

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("entryPoint")]
    public string EntryPoint { get; set; } = string.Empty;

    [JsonProperty("minAppVersion")]
    public string MinAppVersion { get; set; } = "1.0.0";

    public static PluginManifest? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<PluginManifest>(json);
    }
}
