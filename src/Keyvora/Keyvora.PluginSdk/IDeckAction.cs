namespace Keyvora.PluginSdk;

public interface IDeckAction
{
    string TypeId { get; }
    string DisplayName { get; }
    string Description { get; }
    Task ExecuteAsync(DeckActionContext context);
}

public sealed class DeckActionContext
{
    public int ButtonIndex { get; init; }
    public string ProfileName { get; init; } = string.Empty;
    public string? SettingsJson { get; init; }
    public CancellationToken CancellationToken { get; init; }
}
