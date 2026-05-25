namespace Keyvora.PluginSdk;

using System.Text.Json.Serialization;

public sealed class PluginManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "Unknown";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; set; } = string.Empty;

    [JsonPropertyName("minAppVersion")]
    public string MinAppVersion { get; set; } = "1.0.0";

    public static PluginManifest? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<PluginManifest>(json);
    }
}
