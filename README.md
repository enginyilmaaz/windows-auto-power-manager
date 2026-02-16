# Windows Shutdown Helper

A Windows desktop application that schedules system actions like shutdown, restart, sleep, lock, log off, and monitor off based on configurable triggers.

Built with **.NET 8 (WinForms)** and **WebView2** for a modern HTML/CSS/JS-based UI.

[![Build and Release](https://github.com/enginyilmaaz/WindowsShutdownHelper/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/enginyilmaaz/WindowsShutdownHelper/actions/workflows/build-and-release.yml)

## Download

Get the latest installer from the [Releases](https://github.com/enginyilmaaz/WindowsShutdownHelper/releases) page.

## Features

### System Actions
- **Shutdown** - Shuts down the computer
- **Restart** - Restarts the computer
- **Sleep** - Puts the computer to sleep
- **Lock** - Locks the workstation
- **Log Off** - Logs off the current Windows session
- **Turn Off Monitor** - Turns off the display

### Trigger Types
- **System Idle** - Triggers after the system has been idle for a specified duration
- **Countdown (From Now)** - Triggers after a specified amount of time from creation
- **Every Day by Hour (Certain Time)** - Triggers daily at a specific time
- **Bluetooth Lock** - Triggers when a selected Bluetooth device disconnects after previously being reachable

### General
- Up to 5 concurrent actions
- Countdown notifier popup before action execution with options to ignore, delete, or skip
- Pause/resume all actions with preset durations (30m, 1h, 2h, 4h, end of day) or custom duration
- Action conflict validation (prevents duplicate or conflicting actions)
- Search and filter actions by type
- System tray integration with context menu
- Built-in **Help** page with trigger usage guidance (available from hamburger menu, tray menu, and action-list right-click menu)
- Start with Windows (registry-based auto-start)
- Run in background when window is closed
- Action logging with log viewer (filtering, sorting, up to 250 entries)
- Dark/Light/System theme support
- Multi-language support: English, Turkish, German, French, Russian, Italian
- Automatic language detection based on system locale
- Single instance enforcement

## Bluetooth Lock Behavior

The Bluetooth trigger is designed to prevent false one-time locks and now behaves as a disconnect-transition trigger:

1. The selected Bluetooth device must be detected at least once.
2. While the device is reachable, trigger state is considered "connected".
3. When the device changes from reachable to not reachable (based on threshold), the action executes.
4. If the device reconnects and disconnects again, the action executes again.

Notes:
- The threshold is controlled from Settings as **Bluetooth threshold (sec)**.
- This allows repeated lock actions on each real disconnect event instead of only the first disconnect.

## Usage Quick Guide

1. Click **New Action**.
2. Select an action type (Shutdown, Restart, Sleep, Lock, Log Off, Monitor Off).
3. Select a trigger type.
4. Enter trigger value and save.
5. Optionally open **Help** from:
   - Hamburger menu (`Help`)
   - Tray context menu (`Help`)
   - Action table right-click context menu (`Trigger usage help`)

## Tech Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 8.0 (Windows 10+) |
| UI Framework | WinForms + WebView2 |
| Frontend | HTML / CSS / JavaScript (SPA) |
| UI Font | Material Icons (Round) |
| Data Storage | JSON files (settings.json, actionList.json, logs.json) |
| Installer | Inno Setup 6 |
| CI/CD | GitHub Actions |

## Project Structure

```
WindowsShutdownHelper/
├── src/
│   ├── Program.cs                    # Entry point, single instance check
│   ├── MainForm.cs                   # Main form, WebView2 host, timer logic
│   ├── SubWindow.cs                  # Settings/Logs/Help/About sub-windows
│   ├── ActionCountdownNotifier.cs    # Countdown popup before action execution
│   ├── Settings.cs                   # Settings model
│   ├── BuildInfo.cs                  # Build commit ID (injected by CI)
│   ├── Config/
│   │   ├── ActionTypes.cs            # Action type constants
│   │   ├── TriggerTypes.cs           # Trigger type constants
│   │   ├── SettingsINI.cs            # Default settings
│   │   ├── LanguageINI.cs            # Language model
│   │   └── Lang/                     # Language files
│   │       ├── English.cs
│   │       ├── Turkish.cs
│   │       ├── German.cs
│   │       ├── French.cs
│   │       ├── Russian.cs
│   │       └── Italian.cs
│   ├── Functions/
│   │   ├── Actions.cs                # System action execution (Win32 API calls)
│   │   ├── BluetoothScanner.cs       # BLE device scanning and monitoring (WinRT)
│   │   ├── SystemIdleDetector.cs     # User idle time detection (Win32 API)
│   │   ├── DetectScreen.cs           # Session lock/unlock detection
│   │   ├── NotifySystem.cs           # Countdown notification logic
│   │   ├── Logger.cs                 # JSON-based logging
│   │   ├── JsonWriter.cs             # JSON file writer
│   │   ├── StartWithWindows.cs       # Windows startup registry management
│   │   ├── LanguageSelector.cs       # Language detection and loading
│   │   ├── ActionValidation.cs       # Action conflict validation
│   │   ├── ModernMenuRenderer.cs     # Custom tray menu renderer
│   │   └── WebViewEnvironmentProvider.cs # WebView2 environment singleton
│   ├── Enums/                        # UI-related enumerations
│   └── WebView/                      # Frontend assets
│       ├── Index.html                # Main SPA page
│       ├── SubWindow.html            # Sub-window SPA page
│       ├── Countdown.html            # Countdown notifier page
│       ├── Css/
│       │   ├── Style.css             # Main stylesheet
│       │   └── Countdown.css         # Countdown popup styles
│       ├── Fonts/
│       │   └── MaterialIconsRound.otf
│       └── Js/
│           ├── App.js                # SPA router
│           ├── Bridge.js             # C# ↔ JS bridge
│           ├── SubWindow.js          # Sub-window router
│           ├── Countdown.js          # Countdown UI logic
│           ├── Components/
│           │   └── Toast.js          # Toast notification component
│           └── Pages/
│               ├── Main.js           # Action list page
│               ├── Settings.js       # Settings page
│               ├── Logs.js           # Log viewer page
│               ├── Help.js           # Help and trigger usage page
│               └── About.js          # About page
├── tools/
│   ├── dotnet/                       # Local .NET 8.0 SDK
│   ├── create-build.ps1              # PowerShell build script
│   └── create-build.sh               # Bash build script
├── installer.iss                     # Inno Setup installer script
├── Windows Shutdown Helper.csproj    # .NET project file
├── Windows Shutdown Helper.sln       # Solution file
└── .github/
    └── workflows/
        └── build-and-release.yml     # CI/CD pipeline
```

## Building

### Prerequisites
- .NET 8.0 SDK (a local copy is included in `tools/dotnet/`)
- Windows 10 or later
- WebView2 Runtime (included in modern Windows)

### Build

Using the local .NET SDK from `tools/dotnet/` (Linux/macOS shell):
```bash
DOTNET_ROOT="$PWD/tools/dotnet" "$PWD/tools/dotnet/dotnet" restore "Windows Shutdown Helper.sln"
DOTNET_ROOT="$PWD/tools/dotnet" "$PWD/tools/dotnet/dotnet" build "Windows Shutdown Helper.sln" -c Release
```

Using the local .NET SDK from `tools/dotnet/` (Windows PowerShell):
```powershell
$env:DOTNET_ROOT = "$PWD\tools\dotnet"
.\tools\dotnet\dotnet.exe restore "Windows Shutdown Helper.sln"
.\tools\dotnet\dotnet.exe build "Windows Shutdown Helper.sln" -c Release
```

Or if .NET 8.0 SDK is installed globally:
```bash
dotnet restore "Windows Shutdown Helper.sln"
dotnet build "Windows Shutdown Helper.sln" -c Release
```

### Publish (self-contained x64)
```bash
dotnet publish "Windows Shutdown Helper.csproj" -c Release -r win-x64 --self-contained false -p:PublishReadyToRun=true
```

### Create Installer
Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php):
```bash
iscc installer.iss
```

## CI/CD

The GitHub Actions workflow (`.github/workflows/build-and-release.yml`) runs on pushes to `master` and `dev`:

- **dev branch**: Builds the project, creates the installer, and uploads artifacts
- **master branch**: Builds, creates installer, auto-increments the patch version from the latest git tag, and publishes a GitHub Release

## Architecture

The application uses a **hybrid architecture**:
- **Backend (C#)**: WinForms host with WebView2 controls. Handles system actions via Win32 API calls (`user32.dll`, `PowrProf.dll`), timer-based action scheduling, idle detection, session monitoring, settings/action/log persistence in JSON files, and Windows registry management for auto-start.
- **Frontend (HTML/JS)**: Single Page Application rendered inside WebView2. Pages are lazy-loaded as separate JS modules. Communication between C# and JS happens via `PostWebMessageAsJson` / `WebMessageReceived` message passing through a Bridge layer.
- **Sub-windows** (Settings, Logs, Help, About) run in separate WebView2 forms, prewarmed in background for faster opening.

## License

Copyright 2020 enginyilmaaz
