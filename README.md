# Custom Mini Keyvora

![Build](https://github.com/Xtrayzp68/Keyvora/actions/workflows/build.yml/badge.svg)
[![Download Latest](https://img.shields.io/github/v/release/Xtrayzp68/Keyvora?label=Download)](https://github.com/Xtrayzp68/Keyvora/releases/latest)

A custom 6-button USB mini Keyvora powered by an ATmega32U4 (Arduino Pro Micro) with a modern C# / WPF desktop application.

## Features

- 6 mechanical MX-style buttons
- Real-time USB Serial communication (115200 baud)
- Modern dark-theme WPF UI
- Drag & drop action assignment
- Profiles system with hot-switching
- Built-in actions:
  - Keyboard shortcuts
  - Launch applications
  - Open files/folders
  - Spotify control (play/pause/next/prev/volume)
  - Macros (multi-step sequences)
  - Type text
- Plugin SDK for extensibility
- Clean MVVM architecture with event bus

## Hardware

- **MCU:** ATmega32U4 (Arduino Pro Micro, 5V/16MHz)
- **Buttons:** 6x MX-style mechanical switches, INPUT_PULLUP
- **Pin map:** BTN_1→Pin 2, BTN_2→Pin 3, BTN_3→Pin 4,
              BTN_4→Pin 5, BTN_5→Pin 6, BTN_6→Pin 7
- **Communication:** USB Serial CDC, 115200 baud

## Project Structure

```
Keyvora/
├── firmware/                    # Arduino firmware
│   └── KeyvoraFirmware.ino
├── src/
│   └── Keyvora/
│       ├── Keyvora.Desktop/  # WPF desktop application
│       └── Keyvora.PluginSdk/ # Plugin SDK
├── plugins/                     # User-installed plugins
├── docs/
│   ├── ARCHITECTURE.md
│   ├── PROTOCOL.md
│   └── PLUGIN_API.md
└── tests/
    └── Keyvora.Desktop.Tests/
```

## Building

### Firmware

Open `firmware/KeyvoraFirmware/KeyvoraFirmware.ino` in Arduino IDE.
Select board: **Arduino Micro** (ATmega32U4).
Upload at 115200 baud.

### Desktop App

**Option 1 — Télécharger le .exe pré-build**  
Le dernier `.exe` est buildé automatiquement par GitHub Actions à chaque push.  
Va sur [Actions](https://github.com/Xtrayzp68/Keyvora/actions) ou [Releases](https://github.com/Xtrayzp68/Keyvora/releases/latest) pour le télécharger.

**Option 2 — Builder soi-même**

Requires .NET 8 SDK.

```bash
cd src/Keyvora
dotnet restore
dotnet build -c Release
dotnet run --project Keyvora.Desktop
```

## Roadmap

### v1.0 (Current)
- [x] Arduino firmware with debounce
- [x] Desktop app with dark theme UI
- [x] Drag & drop action assignment
- [x] Profiles (add/remove/switch)
- [x] Built-in actions (keyboard, launch, file, Spotify, macros, text)
- [x] Plugin system SDK
- [x] Event-driven architecture
- [x] Spotify OAuth + playback control

### v1.1
- [ ] USB auto-detect (VID/PID matching)
- [ ] RGB LED control protocol + firmware
- [ ] Action icons/images on buttons
- [ ] Advanced macro editor

### v1.5
- [ ] OLED display support
- [ ] Rotary encoder support
- [ ] Pages/folders
- [ ] OBS Studio integration

### v2.0
- [ ] Game integrations
- [ ] Discord Rich Presence
- [ ] Multi-device support
- [ ] Cloud profile sync

## License

MIT
