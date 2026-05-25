namespace Keyvora.PluginSdk;

public abstract class DeckActionBase : IDeckAction
{
    public abstract string TypeId { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract Task ExecuteAsync(DeckActionContext context);
}
