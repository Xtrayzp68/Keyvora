namespace Keyvora.Desktop.Plugins;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Keyvora.Desktop.Actions;
using Keyvora.Desktop.Events;
using Keyvora.Desktop.Profiles;

public sealed class PluginLoader : IDisposable
{
    private readonly string _pluginsDirectory;
    private readonly PluginContext _context;
    private readonly ActionRegistry _actionRegistry;
    private readonly List<PluginInstance> _instances = new();

    private sealed class PluginInstance
    {
        public IPlugin Plugin { get; init; } = null!;
        public AssemblyLoadContext LoadContext { get; init; } = null!;
        public string DirectoryPath { get; init; } = string.Empty;
    }

    public PluginLoader(
        string pluginsDirectory,
        PluginContext context,
        ActionRegistry actionRegistry)
    {
        _pluginsDirectory = pluginsDirectory;
        _context = context;
        _actionRegistry = actionRegistry;
        Directory.CreateDirectory(_pluginsDirectory);
    }

    public void LoadAllPlugins()
    {
        foreach (var dir in Directory.GetDirectories(_pluginsDirectory))
        {
            LoadPlugin(dir);
        }
    }

    public bool LoadPlugin(string directory)
    {
        try
        {
            var manifestPath = Path.Combine(directory, "manifest.json");
            var manifest = PluginManifest.Load(manifestPath);
            if (manifest == null) return false;

            var assemblyPath = Path.Combine(directory, manifest.EntryPoint);
            if (!File.Exists(assemblyPath)) return false;

            var loadContext = new AssemblyLoadContext(manifest.Id, true);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            foreach (var type in assembly.GetExportedTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    if (Activator.CreateInstance(type) is IPlugin plugin)
                    {
                        var dataDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Keyvora",
                            "plugins",
                            manifest.Id);

                        var pluginCtx = new PluginContext(
                            _context.EventBus,
                            _context.ProfileManager,
                            directory,
                            dataDir);

                        plugin.Initialize(pluginCtx);

                        foreach (var action in plugin.GetActions())
                        {
                            _actionRegistry.Register(action);
                        }

                        _instances.Add(new PluginInstance
                        {
                            Plugin = plugin,
                            LoadContext = loadContext,
                            DirectoryPath = directory
                        });
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void UnloadAll()
    {
        foreach (var instance in _instances)
        {
            try
            {
                instance.Plugin.Shutdown();
                instance.LoadContext.Unload();
            }
            catch { /* best effort unload */ }
        }
        _instances.Clear();
    }

    public void Dispose()
    {
        UnloadAll();
    }
}
