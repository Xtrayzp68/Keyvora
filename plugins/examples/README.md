# Example Plugin

This directory is for user-installed plugins.

Each plugin should be in its own subdirectory with:
- `manifest.json`
- The compiled DLL(s)

## Minimal Example

```
plugins/
└── my-plugin/
    ├── manifest.json
    └── MyPlugin.dll
```

See `docs/PLUGIN_API.md` for full documentation.
