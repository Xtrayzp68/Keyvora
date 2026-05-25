namespace Keyvora.Desktop.Actions.BuiltIn;

using System.Threading.Tasks;
using Newtonsoft.Json;
using Keyvora.Desktop.Services;

public sealed class StreamlabsAction : ActionBase
{
    public override string TypeId => "builtin.streamlabs";
    public override string DisplayName => "Streamlabs / OBS";
    public override string Description => "Control Streamlabs Desktop or OBS Studio (scenes, sources, streaming, recording)";

    private static StreamlabsService? _sharedService;

    internal static void Initialize(StreamlabsService? service) => _sharedService = service;

    private StreamlabsService? Service => _sharedService;

    public StreamlabsAction()
    {
        Config = new StreamlabsActionConfig();
    }

    public override async Task ExecuteAsync(IActionContext context)
    {
        if (Config is not StreamlabsActionConfig cfg || Service == null)
            return;

        if (!Service.IsConnected)
            return;

        switch (cfg.Command)
        {
            case "switchscene":
                if (!string.IsNullOrWhiteSpace(cfg.SceneName))
                    await Service.SetCurrentSceneAsync(cfg.SceneName);
                break;
            case "togglesource":
                if (!string.IsNullOrWhiteSpace(cfg.SceneName) && !string.IsNullOrWhiteSpace(cfg.SourceName))
                    await Service.ToggleSourceAsync(cfg.SceneName, cfg.SourceName);
                break;
            case "startstream":
                await Service.StartStreamAsync();
                break;
            case "stopstream":
                await Service.StopStreamAsync();
                break;
            case "togglestream":
                await Service.ToggleStreamAsync();
                break;
            case "startrecord":
                await Service.StartRecordAsync();
                break;
            case "stoprecord":
                await Service.StopRecordAsync();
                break;
            case "togglerecord":
                await Service.ToggleRecordAsync();
                break;
        }
    }
}

public sealed class StreamlabsActionConfig : IActionConfig
{
    [JsonProperty("command")]
    public string Command { get; set; } = "switchscene";

    [JsonProperty("sceneName")]
    public string SceneName { get; set; } = string.Empty;

    [JsonProperty("sourceName")]
    public string SourceName { get; set; } = string.Empty;

    public string Serialize() => JsonConvert.SerializeObject(this);
    public void Deserialize(string json) => JsonConvert.PopulateObject(json, this);
}
