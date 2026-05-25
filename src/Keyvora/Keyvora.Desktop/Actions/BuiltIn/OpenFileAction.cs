namespace Keyvora.Desktop.Actions.BuiltIn;

using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;

public sealed class OpenFileAction : ActionBase
{
    public override string TypeId => "builtin.openfile";
    public override string DisplayName => "Open File";
    public override string Description => "Open a file or folder";

    public OpenFileAction()
    {
        Config = new OpenFileConfig();
    }

    public override async Task ExecuteAsync(IActionContext context)
    {
        if (Config is not OpenFileConfig cfg || string.IsNullOrWhiteSpace(cfg.FilePath))
            return;

        await Task.Run(() =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = cfg.FilePath,
                UseShellExecute = true
            });
        }, context.CancellationToken);
    }
}

public sealed class OpenFileConfig : IActionConfig
{
    [JsonProperty("filePath")]
    public string FilePath { get; set; } = string.Empty;

    public string Serialize() => JsonConvert.SerializeObject(this);
    public void Deserialize(string json) => JsonConvert.PopulateObject(json, this);
}
