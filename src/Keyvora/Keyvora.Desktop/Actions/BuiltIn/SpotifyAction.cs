namespace Keyvora.Desktop.Actions.BuiltIn;

using System.Threading.Tasks;
using Newtonsoft.Json;
using Keyvora.Desktop.Services;

public sealed class SpotifyAction : ActionBase
{
    public override string TypeId => "builtin.spotify";
    public override string DisplayName => "Spotify Control";
    public override string Description => "Control Spotify playback";

    private static SpotifyService? _sharedSpotify;

    internal static void Initialize(SpotifyService? service) => _sharedSpotify = service;

    private SpotifyService? Spotify => _sharedSpotify;

    public SpotifyAction()
    {
        Config = new SpotifyActionConfig();
    }

    public override async Task ExecuteAsync(IActionContext context)
    {
        if (Config is not SpotifyActionConfig cfg || Spotify == null)
            return;

        var cmd = cfg.Command.ToLowerInvariant().Replace(" ", "");
        switch (cmd)
        {
            case "playpause":
            case "play/pause":
                await Spotify.PlayPauseAsync();
                break;
            case "next":
            case "nexttrack":
                await Spotify.NextTrackAsync();
                break;
            case "previous":
            case "previoustrack":
                await Spotify.PreviousTrackAsync();
                break;
            case "volumeup":
                await Spotify.SetVolumeAsync(Math.Min(100, await Spotify.GetVolumeAsync() + 10));
                break;
            case "volumedown":
                await Spotify.SetVolumeAsync(Math.Max(0, await Spotify.GetVolumeAsync() - 10));
                break;
        }
    }
}

public sealed class SpotifyActionConfig : IActionConfig
{
    [JsonProperty("command")]
    public string Command { get; set; } = "playpause";

    public string Serialize() => JsonConvert.SerializeObject(this);
    public void Deserialize(string json) => JsonConvert.PopulateObject(json, this);
}
