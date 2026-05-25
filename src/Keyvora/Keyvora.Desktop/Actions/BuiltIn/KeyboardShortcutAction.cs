namespace Keyvora.Desktop.Actions.BuiltIn;

using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Keyvora.Desktop.Services;
using Newtonsoft.Json;

public sealed class KeyboardShortcutAction : ActionBase
{
    public override string TypeId => "builtin.keyboard";
    public override string DisplayName => "Keyboard Shortcut";
    public override string Description => "Simulate one or more keyboard shortcuts";

    public KeyboardShortcutAction()
    {
        Config = new KeyboardShortcutConfig();
    }

    public override async Task ExecuteAsync(IActionContext context)
    {
        if (Config is not KeyboardShortcutConfig cfg || string.IsNullOrWhiteSpace(cfg.Keys))
            return;

        await Task.Run(() =>
        {
            var modifiers = ModifierKeys.None;
            var keys = cfg.Keys.Split('+');

            foreach (var part in keys)
        {
                var trimmed = part.Trim().ToUpperInvariant();
                switch (trimmed)
                {
                    case "CTRL": case "CONTROL": modifiers |= ModifierKeys.Control; break;
                    case "ALT": modifiers |= ModifierKeys.Alt; break;
                    case "SHIFT": modifiers |= ModifierKeys.Shift; break;
                    case "WIN": case "WINDOWS": case "CMD": modifiers |= ModifierKeys.Windows; break;
                }
            }

            var mainKey = keys[^1].Trim();
            if (Enum.TryParse<Key>(mainKey, ignoreCase: true, out var key))
            {
                KeyboardSimulator.SendKey(key, modifiers);
            }
        }, context.CancellationToken);
    }
}

public sealed class KeyboardShortcutConfig : IActionConfig
{
    [JsonProperty("keys")]
    public string Keys { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    public string Serialize() => JsonConvert.SerializeObject(this);
    public void Deserialize(string json) => JsonConvert.PopulateObject(json, this);
}
