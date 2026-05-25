namespace Keyvora.PluginSdk;

public interface IDeckPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Author { get; }
    void Initialize(PluginContext context);
    void Shutdown();
    IReadOnlyList<IDeckAction> GetActions();
}
