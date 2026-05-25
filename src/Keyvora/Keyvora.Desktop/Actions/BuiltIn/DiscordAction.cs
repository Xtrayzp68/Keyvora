namespace Keyvora.Desktop.Actions.BuiltIn;

using System.Threading.Tasks;
using Newtonsoft.Json;
using Keyvora.Desktop.Services;

public sealed class DiscordAction : ActionBase
{
    public override string TypeId => "builtin.discord";
    public override string DisplayName => "Discord Voice";
    public override string Description => "Control Discord voice (mute, deafen, leave voice channel)";

    private static DiscordService? _sharedService;

    internal static void Initialize(DiscordService? service) => _sharedService = service;

    private DiscordService? Service => _sharedService;

    public DiscordAction()
    {
        Config = new DiscordActionConfig();
    }

    public override async Task ExecuteAsync(IActionContext context)
    {
        if (Config is not DiscordActionConfig cfg || Service == null)
            return;

        if (!Service.IsConnected)
            return;

        switch (cfg.Command)
        {
            case "mute":
                await Service.SetMuteAsync(true);
                break;
            case "unmute":
                await Service.SetMuteAsync(false);
                break;
            case "togglemute":
                await Service.ToggleMuteAsync();
                break;
            case "deafen":
                await Service.SetDeafenAsync(true);
                break;
            case "undeafen":
                await Service.SetDeafenAsync(false);
                break;
            case "toggledeafen":
                await Service.ToggleDeafenAsync();
                break;
            case "leavevoice":
                await Service.LeaveVoiceChannelAsync();
                break;
        }
    }
}

public sealed class DiscordActionConfig : IActionConfig
{
    [JsonProperty("command")]
    public string Command { get; set; } = "togglemute";

    public string Serialize() => JsonConvert.SerializeObject(this);
    public void Deserialize(string json) => JsonConvert.PopulateObject(json, this);
}
