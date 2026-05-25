namespace Keyvora.PluginSdk;

public sealed class PluginContext
{
    public string PluginDirectory { get; }
    public string DataDirectory { get; }
    public EventBus EventBus { get; }

    public PluginContext(string pluginDirectory, string dataDirectory)
    {
        PluginDirectory = pluginDirectory;
        DataDirectory = dataDirectory;
        EventBus = new EventBus();
    }
}
