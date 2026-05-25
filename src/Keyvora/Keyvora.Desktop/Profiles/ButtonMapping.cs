namespace Keyvora.Desktop.Profiles;

using Newtonsoft.Json;
using Keyvora.Desktop.Actions;

public sealed class ButtonMapping
{
    [JsonProperty("actionTypeId")]
    public string ActionTypeId { get; set; } = string.Empty;

    [JsonProperty("config")]
    public string? ActionConfigJson { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; } = string.Empty;

    [JsonProperty("iconType")]
    public string IconType { get; set; } = "None";

    [JsonProperty("icon")]
    public string? IconPath { get; set; }

    [JsonProperty("backgroundColor")]
    public string BackgroundColor { get; set; } = "#1E1E1E";

    [JsonProperty("imageScale")]
    public double ImageScale { get; set; } = 1.0;

    [JsonProperty("imageOffsetX")]
    public double ImageOffsetX { get; set; } = 0;

    [JsonProperty("imageOffsetY")]
    public double ImageOffsetY { get; set; } = 0;

    [JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    public ButtonMapping() { }

    public ButtonMapping(string actionTypeId, string? config = null)
    {
        ActionTypeId = actionTypeId;
        ActionConfigJson = config;
    }

    public T? DeserializeConfig<T>() where T : class, IActionConfig
    {
        if (string.IsNullOrWhiteSpace(ActionConfigJson)) return null;
        var config = Activator.CreateInstance<T>();
        config.Deserialize(ActionConfigJson);
        return config;
    }
}
