# Keyvora Plugin SDK

## Getting Started

Create a new .NET 8 Class Library project:

```bash
dotnet new classlib -n MyPlugin --framework net8.0
```

Add a reference to the SDK:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Keyvora.PluginSdk.csproj" />
</ItemGroup>
```

## Plugin Structure

Every plugin needs:

```
my-plugin/
├── manifest.json
├── MyPlugin.dll
└── (dependencies)
```

### manifest.json

```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "author": "You",
  "description": "Does awesome things",
  "entryPoint": "MyPlugin.dll",
  "minAppVersion": "1.0.0"
}
```

### Plugin Class

```csharp
using Keyvora.PluginSdk;

public class MyPlugin : IDeckPlugin
{
    public string Id => "my-plugin";
    public string Name => "My Plugin";
    public string Version => "1.0.0";
    public string Author => "You";

    private PluginContext _context = null!;

    public void Initialize(PluginContext context)
    {
        _context = context;
        // Subscribe to events, set up resources
    }

    public void Shutdown()
    {
        // Clean up resources
    }

    public IReadOnlyList<IDeckAction> GetActions()
    {
        return new List<IDeckAction> { new MyAction() };
    }
}

public class MyAction : DeckActionBase
{
    public override string TypeId => "my-plugin.myaction";
    public override string DisplayName => "My Action";
    public override string Description => "What my action does";

    public override async Task ExecuteAsync(DeckActionContext context)
    {
        // Your action logic here
        await Task.CompletedTask;
    }
}
```

## API Reference

### IDeckPlugin
- `Initialize(PluginContext)` — Called when plugin is loaded
- `Shutdown()` — Called when app exits or plugin is unloaded
- `GetActions()` — Returns list of actions this plugin provides

### DeckActionBase
- `TypeId` — Unique identifier (convention: `plugin-name.action-name`)
- `DisplayName` — Shown in UI
- `Description` — Tooltip text
- `ExecuteAsync(DeckActionContext)` — Called when button is pressed

### DeckActionContext
- `ButtonIndex` — Which button triggered (1-6)
- `ProfileName` — Current active profile
- `SettingsJson` — Serialized config from user
- `CancellationToken` — Respect cancellation

## Distribution

Copy your plugin folder to `{app}/plugins/`.
The app auto-discovers plugins on startup.
