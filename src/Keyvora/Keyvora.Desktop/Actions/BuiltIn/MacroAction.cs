namespace Keyvora.Desktop.Actions.BuiltIn;

using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

public sealed class MacroAction : ActionBase
{
    public override string TypeId => "builtin.macro";
    public override string DisplayName => "Macro";
    public override string Description => "Execute a sequence of actions";

    private static ActionRegistry? _registry;

    public static void Initialize(ActionRegistry registry) => _registry = registry;

    public MacroAction()
    {
        Config = new MacroConfig();
    }

    public override async Task ExecuteAsync(IActionContext context)
    {
        if (Config is not MacroConfig cfg || cfg.Steps.Count == 0)
            return;

        int loopCount = cfg.Loop ? cfg.LoopCount : 1;
        for (int l = 0; l < loopCount; l++)
        {
            foreach (var step in cfg.Steps)
            {
                if (context.CancellationToken.IsCancellationRequested) break;
                if (step.DelayMs > 0)
                    await Task.Delay(step.DelayMs, context.CancellationToken);

                var action = _registry?.Get(step.ActionTypeId);
                if (action == null) continue;

                if (step.ConfigJson != null && action.Config != null)
                {
                    action.Config.Deserialize(step.ConfigJson);
                }

                await action.ExecuteAsync(context);
            }
            if (context.CancellationToken.IsCancellationRequested) break;
        }
    }
}

public sealed class MacroStep
{
    [JsonProperty("actionTypeId")]
    public string ActionTypeId { get; set; } = string.Empty;

    [JsonProperty("config")]
    public string? ConfigJson { get; set; }

    [JsonProperty("delayMs")]
    public int DelayMs { get; set; }
}

public sealed class MacroConfig : IActionConfig
{
    [JsonProperty("steps")]
    public List<MacroStep> Steps { get; set; } = new();

    [JsonProperty("loop")]
    public bool Loop { get; set; }

    [JsonProperty("loopCount")]
    public int LoopCount { get; set; } = 1;

    public string Serialize() => JsonConvert.SerializeObject(this);
    public void Deserialize(string json) => JsonConvert.PopulateObject(json, this);
}
