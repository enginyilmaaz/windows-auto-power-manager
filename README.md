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

### General
- Up to 5 concurrent actions
- Countdown notifier popup before action execution with options to ignore, delete, or skip
- Pause/resume all actions with preset durations (30m, 1h, 2h, 4h, end of day) or custom duration
- Action conflict validation (prevents duplicate or conflicting actions)
- Search and filter actions by type
- System tray integration with context menu
- Start with Windows (registry-based auto-start)
- Run in background when window is closed
- Action logging with log viewer (filtering, sorting, up to 250 entries)
- Dark/Light/System theme support
- Multi-language support: English, Turkish, German, French, Russian, Italian
- Automatic language detection based on system locale
- Single instance enforcement

## Tech Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 8.0 (Windows) |
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
│   ├── SubWindow.cs                  # Settings/Logs/About sub-windows
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
│   └── wwwroot/                      # Frontend assets
│       ├── index.html                # Main SPA page
│       ├── subwindow.html            # Sub-window SPA page
│       ├── countdown.html            # Countdown notifier page
│       ├── css/
│       │   ├── style.css             # Main stylesheet
│       │   └── countdown.css         # Countdown popup styles
│       └── js/
│           ├── app.js                # SPA router
│           ├── bridge.js             # C# ↔ JS bridge
│           ├── subwindow.js          # Sub-window router
│           ├── countdown.js          # Countdown UI logic
│           ├── components/
│           │   └── toast.js          # Toast notification component
│           └── pages/
│               ├── main.js           # Action list page
│               ├── settings.js       # Settings page
│               ├── logs.js           # Log viewer page
│               └── about.js          # About page
├── installer.iss                     # Inno Setup installer script
├── Windows Shutdown Helper.csproj    # .NET project file
├── Windows Shutdown Helper.sln       # Solution file
└── .github/
    └── workflows/
        └── build-and-release.yml     # CI/CD pipeline
```

## Building

### Prerequisites
- .NET 8.0 SDK
- Windows 10 or later
- WebView2 Runtime (included in modern Windows)

### Build
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
- **Sub-windows** (Settings, Logs, About) run in separate WebView2 forms, prewarmed in background for instant opening.

## License

Copyright 2020 enginyilmaaz
