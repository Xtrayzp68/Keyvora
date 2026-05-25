namespace Keyvora.Desktop.Actions.BuiltIn;

using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json;

public sealed class PlaySoundAction : ActionBase
{
    public override string TypeId => "builtin.playsound";
    public override string DisplayName => "Play Sound";
    public override string Description => "Play an audio file through speakers and microphone (e.g. Wave Link SFX, VB-Cable)";

    public PlaySoundAction()
    {
        Config = new PlaySoundConfig();
    }

    public override async Task ExecuteAsync(IActionContext context)
    {
        if (Config is not PlaySoundConfig cfg || string.IsNullOrWhiteSpace(cfg.FilePath))
            return;

        await Task.Run(() =>
        {
            try
            {
                using var reader = new AudioFileReader(cfg.FilePath);
                using var output = new WaveOutEvent();

                if (cfg.OutputDeviceId >= 0)
                {
                    output.DeviceNumber = cfg.OutputDeviceId;
                }

                output.Init(reader);
                output.Play();

                while (output.PlaybackState == PlaybackState.Playing)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        output.Stop();
                        break;
                    }
                    Task.Delay(100, context.CancellationToken).Wait();
                }
            }
            catch
            {
            }
        }, context.CancellationToken);
    }
}

public sealed class PlaySoundConfig : IActionConfig
{
    [JsonProperty("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonProperty("outputDeviceId")]
    public int OutputDeviceId { get; set; } = -1;

    public string Serialize() => JsonConvert.SerializeObject(this);
    public void Deserialize(string json) => JsonConvert.PopulateObject(json, this);
}
