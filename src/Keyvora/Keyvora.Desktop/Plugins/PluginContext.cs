namespace Keyvora.Desktop.Plugins;

using System.IO;
using Keyvora.Desktop.Events;
using Keyvora.Desktop.Profiles;

public sealed class PluginContext
{
    public IEventBus EventBus { get; }
    public ProfileManager ProfileManager { get; }
    public string PluginDirectory { get; }
    public string DataDirectory { get; }

    public PluginContext(
        IEventBus eventBus,
        ProfileManager profileManager,
        string pluginDirectory,
        string dataDirectory)
    {
        EventBus = eventBus;
        ProfileManager = profileManager;
        PluginDirectory = pluginDirectory;
        DataDirectory = dataDirectory;

        Directory.CreateDirectory(dataDirectory);
    }
}
