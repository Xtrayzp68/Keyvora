namespace Keyvora.Desktop.Actions.BuiltIn;

using System.Threading.Tasks;
using Newtonsoft.Json;
using Keyvora.Desktop.Services;

public sealed class TextAction : ActionBase
{
    public override string TypeId => "builtin.text";
    public override string DisplayName => "Type Text";
    public override string Description => "Type a string of text";

    public TextAction()
    {
        Config = new TextActionConfig();
    }

    public override async Task ExecuteAsync(IActionContext context)
    {
        if (Config is not TextActionConfig cfg || string.IsNullOrWhiteSpace(cfg.Text))
            return;

        await Task.Run(() =>
        {
            KeyboardSimulator.TypeText(cfg.Text);
            if (cfg.PressEnter)
            {
                KeyboardSimulator.SendKey(System.Windows.Input.Key.Enter);
            }
        }, context.CancellationToken);
    }
}

public sealed class TextActionConfig : IActionConfig
{
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("pressEnter")]
    public bool PressEnter { get; set; }

    public string Serialize() => JsonConvert.SerializeObject(this);
    public void Deserialize(string json) => JsonConvert.PopulateObject(json, this);
}
