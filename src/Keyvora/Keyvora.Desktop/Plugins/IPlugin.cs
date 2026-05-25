namespace Keyvora.Desktop.Plugins;

using Keyvora.Desktop.Actions;

public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Author { get; }
    string Description { get; }
    void Initialize(PluginContext context);
    void Shutdown();
    IReadOnlyList<IAction> GetActions();
}
