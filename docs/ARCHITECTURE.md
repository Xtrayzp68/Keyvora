# Keyvora — Architecture Document

## Overview

A custom 6-button USB mini Keyvora, inspired by Elgato Keyvora.
Lightweight ATmega32U4 firmware + high-performance C# / .NET 8 WPF desktop application.

## Tech Stack

| Layer | Choice | Rationale |
|-------|--------|-----------|
| **Desktop App** | C# 12 / .NET 8 / WPF | Native Windows perf, MVVM with CommunityToolkit, best-in-class drag & drop, XAML hot reload |
| **Firmware** | Arduino (C++) | Minimal footprint, event-driven, debounce only |
| **Serial** | USB CDC (115200 baud) | Standard, reliable, cross-platform |
| **Plugin SDK** | .NET Class Library | AssemblyLoadContext isolation, hot-loadable |
| **Spotify API** | SpotifyAPI.Web + OAuth | Official NuGet package |
| **Keyboard** | Win32 keybd_event | Low-level, no dependencies |
| **DI / Events** | Custom EventBus | Lightweight pub/sub, no framework overhead |

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer (WPF)                        │
│  MainWindow / ButtonGrid / ActionEditor / KeyvoraButton   │
│  MVVM: CommunityToolkit.Mvvm (ObservableObject, RelayCommand)│
├─────────────────────────────────────────────────────────────┤
│                    ViewModel Layer                            │
│  MainViewModel / ButtonGridViewModel / ButtonViewModel       │
│  ProfileSelectorViewModel / ActionEditorViewModel            │
├─────────────────────────────────────────────────────────────┤
│                    Service Layer                              │
│  SpotifyService / KeyboardSimulator / ProcessLauncher        │
├─────────────────────────────────────────────────────────────┤
│                   Application Core                           │
│  EventBus / ActionRegistry / ProfileManager / PluginLoader   │
├─────────────────────────────────────────────────────────────┤
│                   Hardware Layer                              │
│  SerialDeviceManager / SerialProtocol / IDeviceManager       │
├─────────────────────────────────────────────────────────────┤
│                      Arduino Firmware                         │
│  KeyvoraFirmware.ino (event-driven button reader)         │
└─────────────────────────────────────────────────────────────┘
```

## Event Flow

```
Arduino Button Press
  → USB Serial: "BTN_1\n"
  → SerialDeviceManager (background thread)
  → EventBus.Publish(ButtonPressedEvent)
  → MainViewModel.OnButtonPressed
  → ProfileManager.ActiveProfile.Buttons[index]
  → ActionRegistry.Get(typeId)
  → IAction.ExecuteAsync(context)
```

## Profiles

- Stored as JSON in `%LOCALAPPDATA%/Keyvora/profiles/`
- ProfileManager handles CRUD + activation
- Each profile maps button indices (1-6) → ButtonMapping

## Plugins

- .NET assemblies in `plugins/` directory
- Manifest.json required per plugin
- Loaded via AssemblyLoadContext (isolated, unloadable)
- PluginContext provides EventBus for cross-plugin communication

## Serial Protocol

- One line = one event
- Format: `BTN_N` (press)
- Baud: 115200
- No ACK needed (fire-and-forget)
- Reserved for future: `BTN_N_UP`, `PING`, `PONG`, `RGB_*`

## Action System

| Type ID | Action | Config |
|---------|--------|--------|
| `builtin.keyboard` | Keyboard shortcut | Keys string (Ctrl+Shift+A) |
| `builtin.launch` | Launch app | Path, args, working dir |
| `builtin.openfile` | Open file/folder | File path |
| `builtin.spotify` | Spotify control | Command (playpause/next/prev) |
| `builtin.macro` | Macro sequence | List of steps |
| `builtin.text` | Type text | Text string |
